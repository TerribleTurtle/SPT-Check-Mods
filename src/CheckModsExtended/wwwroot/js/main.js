import { fetchStatus, fetchScan, fetchCache, fetchLocalScan, ignoreMod, unignoreMod, systemOpen, fetchIgnores, fetchSettings, saveSettings } from './api.js';


document.addEventListener('alpine:init', () => {
    /**
     * @typedef {Object} ModManagerData
     */
    Alpine.data('modManager', () => ({
        mods: [],
        filters: { search: '', status: 'all' },
        sort: { column: 'status', direction: 'asc' },
        ui: {
            scanning: false,
            initialLoading: true,
            driftDetected: false,
            localScanController: null,
            selectedIds: new Set(),
            lastFocus: null,
            showIgnoreModal: false,
            showSettingsModal: false,
            showCommunityListModal: false,
            loadingIgnores: false,
            loadingSettings: false,
            isScrolledToBottom: false,
            toasts: [],
            toastIdCounter: 0,
            currentTime: Date.now()
        },
        meta: {
            sptVersion: null,
            appVersion: null,
            appUpdateAvailable: false,
            lastScan: null,
            theme: localStorage.getItem('cme-theme') || 'dark'
        },
        ignores: [],
        settings: null,
        selectedMod: null,
        loaderProgress: 0,
        loaderText: '> Initiating workspace scan…',
        loaderEventSource: null,
        filteredMods: [],

        updateFilteredMods() {
            // Why: We filter and sort the full mods array locally
            let res = this.mods.filter(mod => {
                if (this.filters.search) {
                    const query = this.filters.search.toLowerCase();
                    const matchName = mod.name && mod.name.toLowerCase().includes(query);
                    const matchAuthor = mod.author && mod.author.toLowerCase().includes(query);
                    if (!matchName && !matchAuthor) return false;
                }
                if (this.filters.status !== 'all') {
                    if (this.filters.status === 'ignored') return mod.isIgnored === true;
                    if (mod.isIgnored) return false;
                    if (this.filters.status === 'ok') return mod.status === 'UpToDate' || mod.status === 'NewerInstalled';
                    if (this.filters.status === 'attention') return ['UpdateAvailable', 'UpdateBlocked', 'Incompatible'].includes(mod.status);
                }
                return true;
            });
            
            res.sort((a, b) => {
                let valA, valB;
                if (this.sort.column === 'name') {
                    valA = (a.name || '').toLowerCase();
                    valB = (b.name || '').toLowerCase();
                } else if (this.sort.column === 'status') {
                    if (a.isIgnored && !b.isIgnored) return this.sort.direction === 'asc' ? 1 : -1;
                    if (!a.isIgnored && b.isIgnored) return this.sort.direction === 'asc' ? -1 : 1;
                    const order = { 'UpdateAvailable': 0, 'UpdateBlocked': 1, 'Incompatible': 2, 'Error': 3, 'NoVersionsFound': 4, 'NewerInstalled': 5, 'UpToDate': 6, 'Unknown': 7 };
                    valA = order[a.status] !== undefined ? order[a.status] : 8;
                    valB = order[b.status] !== undefined ? order[b.status] : 8;
                } else if (this.sort.column === 'version') {
                    const parseVer = v => (v || '').split('-')[0].split('.').map(n => parseInt(n) || 0);
                    const pA = parseVer(a.localVersion);
                    const pB = parseVer(b.localVersion);
                    const len = Math.max(pA.length, pB.length);
                    let cmp = 0;
                    for (let i = 0; i < len; i++) {
                        const nA = pA[i] || 0;
                        const nB = pB[i] || 0;
                        if (nA > nB) { cmp = 1; break; }
                        if (nA < nB) { cmp = -1; break; }
                    }
                    return this.sort.direction === 'asc' ? cmp : -cmp;
                } else if (this.sort.column === 'type') {
                    if (this.sort.direction === 'asc' || this.sort.direction === 'paired') {
                        valA = a.isPaired ? 2 : (a.isServerMod ? 1 : 0);
                        valB = b.isPaired ? 2 : (b.isServerMod ? 1 : 0);
                    } else {
                        valA = a.isPaired ? 0 : (a.isServerMod ? 2 : 1);
                        valB = b.isPaired ? 0 : (b.isServerMod ? 2 : 1);
                    }
                } else {
                    valA = ''; valB = '';
                }
                
                if (valA < valB) return this.sort.direction === 'asc' ? -1 : 1;
                if (valA > valB) return this.sort.direction === 'asc' ? 1 : -1;
                
                const nameA = (a.name || '').toLowerCase();
                const nameB = (b.name || '').toLowerCase();
                if (nameA < nameB) return -1;
                if (nameA > nameB) return 1;
                return 0;
            });
            this.filteredMods = res;
        },
        get activeMods() { return this.mods.filter(m => !m.isIgnored); },
        get ignoredMods() { return this.mods.filter(m => m.isIgnored); },
        get updatesAvailable() { return this.activeMods.filter(m => m.status === 'UpdateAvailable'); },
        get updatesBlocked() { return this.activeMods.filter(m => m.status === 'UpdateBlocked'); },
        get incompatibleMods() { return this.activeMods.filter(m => m.status === 'Incompatible'); },
        get attentionMods() { return this.activeMods.filter(m => ['UpdateAvailable', 'UpdateBlocked', 'Incompatible'].includes(m.status)); },
        get upToDateMods() { return this.activeMods.filter(m => ['UpToDate', 'NewerInstalled'].includes(m.status)); },
        get downloadableUpdates() { return this.updatesAvailable.filter(m => m.downloadUrl); },
        get pageUpdates() { return this.updatesAvailable.filter(m => m.modUrl); },
        get isAllSelected() { return this.filteredMods.length > 0 && this.filteredMods.every(m => this.ui.selectedIds.has(String(m.id))); },
        get isIndeterminate() { return this.filteredMods.some(m => this.ui.selectedIds.has(String(m.id))) && !this.isAllSelected; },
        get documentTitle() {
            const outdatedCount = this.updatesAvailable.length + this.updatesBlocked.length;
            return outdatedCount > 0 ? `(${outdatedCount} outdated) Check Mods Extended` : "CheckModsExtended // MANAGER";
        },

        init() {
            this.$watch('mods', () => this.updateFilteredMods());
            this.$watch('filters.search', () => this.updateFilteredMods());
            this.$watch('filters.status', () => this.updateFilteredMods());
            this.$watch('sort.column', () => this.updateFilteredMods());
            this.$watch('sort.direction', () => this.updateFilteredMods());

            document.documentElement.dataset.theme = this.meta.theme;
            const savedFilter = localStorage.getItem('cme-filter-status');
            if (savedFilter) this.filters.status = savedFilter;
            const savedSortCol = localStorage.getItem('cme-sort-column');
            const savedSortDir = localStorage.getItem('cme-sort-direction');
            if (savedSortCol) this.sort.column = savedSortCol;
            if (savedSortDir) this.sort.direction = savedSortDir;

            fetchStatus().then(data => {
                if (data && data.version) this.meta.appVersion = data.version;
                if (data && data.sptVersion) this.meta.sptVersion = data.sptVersion;
                if (data && data.appUpdateAvailable !== undefined) this.meta.appUpdateAvailable = data.appUpdateAvailable;
            }).finally(async () => {
                try {
                    this.settings = await fetchSettings();
                    if (!this.settings.IgnoredUpdateOptions) this.settings.IgnoredUpdateOptions = {};
                    if (this.settings.IgnoredUpdateOptions.UseCommunityList == null) {
                        this.ui.showCommunityListModal = true;
                    }
                } catch (e) {
                    console.error("Failed to fetch settings on init:", e);
                }

                const cacheData = await fetchCache();
                const responseData = cacheData?.response || cacheData?.Response;
                const cachedAt = cacheData?.cachedAtUtc || cacheData?.CachedAtUtc;
                if (cacheData && responseData) {
                    console.log("CACHE DATA RECEIVED:", JSON.stringify(cacheData));
                    this.mods = responseData.mods || responseData.Mods || [];
                    this.meta.lastScan = new Date(cachedAt).getTime();
                    console.log("META LAST SCAN:", this.meta.lastScan);
                    
                    const urlParams = new URLSearchParams(window.location.search);
                    if (urlParams.has('cli')) {
                        console.log("LAUNCHED FROM CLI, SKIPPING BACKGROUND SCAN");
                        window.history.replaceState({}, document.title, window.location.pathname);
                    } else {
                        // Check if cache is older than 24 hours (86,400,000 ms)
                        const cacheAge = Date.now() - this.meta.lastScan;
                        if (cacheAge > 86400000) {
                            this.showToast("Your scan data is older than 24 hours. A fresh scan is recommended.", "warning");
                        }
                        
                        // Run lightweight drift detection
                        setTimeout(() => this.checkDrift(), 1000);
                    }
                } else {
                    console.log("NO CACHE DATA FOUND, RUNNING SCAN");
                    this.handleScan();
                }
                this.ui.initialLoading = false;
            });

            setInterval(() => {
                this.ui.currentTime = Date.now();
            }, 10000);
        },

        toggleTheme() {
            this.meta.theme = this.meta.theme === 'dark' ? 'light' : 'dark';
            document.documentElement.dataset.theme = this.meta.theme;
            localStorage.setItem('cme-theme', this.meta.theme);
        },

        setFilter(status) {
            this.filters.status = status;
            localStorage.setItem('cme-filter-status', status);
            this.ui.selectedIds.clear();
        },

        clearFilters() {
            this.filters.search = '';
            this.setFilter('all');
        },

        setSort(column) {
            if (this.sort.column === column) {
                if (this.sort.direction === 'asc') this.sort.direction = 'desc';
                else if (this.sort.direction === 'desc' && column === 'type') this.sort.direction = 'paired';
                else {
                    this.sort.column = column === 'status' ? 'name' : 'status';
                    this.sort.direction = 'asc';
                }
            } else {
                this.sort.column = column;
                this.sort.direction = 'asc';
            }
            localStorage.setItem('cme-sort-column', this.sort.column);
            localStorage.setItem('cme-sort-direction', this.sort.direction);
        },

        toggleSelect(id, event) {
            if (this.ui.selectedIds.has(id)) this.ui.selectedIds.delete(id);
            else this.ui.selectedIds.add(id);
        },

        toggleSelectAll() {
            if (this.isAllSelected) {
                this.filteredMods.forEach(m => this.ui.selectedIds.delete(String(m.id)));
            } else {
                this.filteredMods.forEach(m => this.ui.selectedIds.add(String(m.id)));
            }
        },

        selectMod(mod) {
            this.selectedMod = mod;
        },


        async checkDrift() {
            // Why: Runs a lightweight hash check to see if local mods changed
            if (this.ui.scanning) return;
            
            this.ui.localScanController = new AbortController();
            try {
                const localData = await fetchLocalScan(this.ui.localScanController.signal);
                if (!localData || !localData.mods) return;
                
                // Diff by ID/Name + Version
                const getModHash = (m) => {
                    return m.localName + m.localVersion;
                };

                const localHash = localData.mods.map(getModHash).sort().join();
                const cachedHash = this.mods.map(getModHash).sort().join();
                
                if (localHash !== cachedHash) {
                    this.ui.driftDetected = true;
                }
            } catch (err) {
                if (err.name !== 'AbortError') {
                    console.error("Drift check failed:", err);
                }
            } finally {
                this.ui.localScanController = null;
            }
        },

        async handleScan() {
            if (this.ui.scanning) return;
            this.ui.scanning = true;
            this.ui.driftDetected = false;
            
            if (this.ui.localScanController) {
                this.ui.localScanController.abort();
            }
            
            this.selectedMod = null;
            this.showToast('Scan started...', 'info');
            this.startLoader();
            try {
                const results = await fetchScan();
                this.mods = results.mods || [];
                this.meta.lastScan = Date.now();
                this.showToast(`Scan complete. ${this.mods.length} entities analyzed.`, 'success');
            } catch (error) {
                this.showToast(`Scan failed: ${error.message}`, 'error');
            } finally {
                this.stopLoader();
                this.ui.scanning = false;
            }
        },

        startLoader() {
            this.loaderProgress = 0;
            this.loaderPhase = 0;
            this._lastRawProgress = 0;
            this.loaderText = '> Initiating workspace scan…';
            this.loaderEventSource = new EventSource('/api/scan/progress');
            this.loaderEventSource.onmessage = (event) => {
                try {
                    const data = JSON.parse(event.data);
                    const text = data.text !== undefined ? data.text : data.Text;
                    const rawProgress = data.progress !== undefined ? data.progress : data.Progress;
                    
                    if (text !== undefined) {
                        this.loaderText = text;
                        const lowerText = text.toLowerCase();
                        if (lowerText.includes('forge')) this.loaderPhase = 0;
                        else if (lowerText.includes('dependencies') || lowerText.includes('resolving')) this.loaderPhase = 1;
                    }
                    
                    if (rawProgress !== undefined && rawProgress !== null) {
                        if (rawProgress < this._lastRawProgress && rawProgress === 0) {
                            if (this.loaderPhase === 0) this.loaderPhase = 1;
                        }
                        this._lastRawProgress = rawProgress;
                        this.loaderProgress = (this.loaderPhase * 50) + (rawProgress / 2);
                    }
                } catch (e) {
                    console.error('Error parsing progress data', e);
                }
            };
        },

        stopLoader() {
            if (this.loaderEventSource) {
                this.loaderEventSource.close();
                this.loaderEventSource = null;
            }
            this.loaderProgress = 100;
            this.loaderText = '> SCAN COMPLETE';
        },

        systemOpen(target) {
            systemOpen(target).catch(e => this.showToast(`Error opening target: ${e.message}`, 'error'));
        },

        async ignoreMod(id, localVer, latestVer, btnEvent) {
            if(btnEvent) btnEvent.target.classList.add('is-loading');
            try {
                await ignoreMod(id, localVer, latestVer);
                this.showToast(`Successfully ignored ${id}.`, 'success');
                this.selectedMod = null;
                this.handleScan();
            } catch (err) {
                this.showToast(`Error ignoring mod: ${err.message}`, 'error');
            }
        },

        async unignoreMod(id, btnEvent, fromModal = false) {
            if(btnEvent) btnEvent.target.classList.add('is-loading');
            try {
                await unignoreMod(id);
                this.showToast(`Successfully un-ignored ${id}.`, 'success');
                if (fromModal) {
                    await this.loadIgnores();
                } else {
                    this.selectedMod = null;
                }
                this.handleScan();
            } catch (err) {
                this.showToast(`Error un-ignoring mod: ${err.message}`, 'error');
            }
        },

        async bulkUpdate(btnEvent) {
            const selectedMods = this.mods.filter(m => this.ui.selectedIds.has(String(m.id)));
            if(btnEvent) btnEvent.target.classList.add('is-loading');
            for (const m of selectedMods) {
                const url = m.downloadUrl || m.modUrl;
                if (url) {
                    try {
                        await systemOpen(url);
                    } catch (e) {
                        this.showToast(`Error opening URL for ${m.name}: ${e}`, 'error');
                    }
                }
            }
            if(btnEvent) btnEvent.target.classList.remove('is-loading');
        },

        async bulkIgnore(btnEvent) {
            const selectedMods = this.mods.filter(m => this.ui.selectedIds.has(String(m.id)));
            if(btnEvent) btnEvent.target.classList.add('is-loading');
            for (const mod of selectedMods) {
                try {
                    await ignoreMod(mod.id, mod.localVersion, mod.latestVersion);
                    this.showToast(`Successfully ignored ${mod.id}.`, 'success');
                } catch(err) {
                    this.showToast(`Error ignoring mod ${mod.id}: ${err.message}`, 'error');
                }
            }
            if(btnEvent) btnEvent.target.classList.remove('is-loading');
            this.ui.selectedIds.clear();
            this.handleScan();
        },

        async bulkDownloadUpdates() {
            for (const m of this.downloadableUpdates) {
                this.systemOpen(m.downloadUrl);
            }
        },

        async bulkOpenPages() {
            for (const m of this.pageUpdates) {
                this.systemOpen(m.modUrl);
            }
        },

        copyModsList(e) {
            const list = this.mods.map(m => `- ${m.name} (v${m.localVersion || 'Unknown'}) - ${m.status}`).join('\n');
            navigator.clipboard.writeText(list).then(() => {
                this.showToast('Copied!', 'success');
            });
        },

        async showIgnoreDashboard() {
            this.ui.lastFocus = document.activeElement;
            this.ui.showIgnoreModal = true;
            await this.loadIgnores();
        },
        
        async loadIgnores() {
            this.ui.loadingIgnores = true;
            try {
                this.ignores = await fetchIgnores();
            } catch (e) {
                this.showToast(`Error loading ignores: ${e.message}`, 'error');
                this.ignores = [];
            } finally {
                this.ui.loadingIgnores = false;
            }
        },

        async showSettingsDashboard() {
            this.ui.lastFocus = document.activeElement;
            this.ui.showSettingsModal = true;
            this.ui.loadingSettings = true;
            try {
                this.settings = await fetchSettings();
                if(!this.settings.AppPaths) this.settings.AppPaths = { AppDataDirectory: '' };
                if(!this.settings.LoggingOptions) this.settings.LoggingOptions = { EnableFileLogging: false, MinimumLogLevel: 'Information', LogFilePath: 'checkmod.log', MaxFileSizeBytes: 10485760 };
                if(!this.settings.IgnoredUpdateOptions) this.settings.IgnoredUpdateOptions = { UseCommunityList: null };
            } catch (e) {
                this.showToast(`Error loading settings: ${e.message}`, 'error');
                this.settings = null;
            } finally {
                this.ui.loadingSettings = false;
            }
        },

        async saveSettings(btnEvent) {
            if(btnEvent) btnEvent.target.classList.add('is-loading');
            try {
                await saveSettings(this.settings);
                this.closeModals();
                this.showToast('Settings saved! A restart may be required.', 'success');
            } catch (e) {
                this.showToast(`Error saving settings: ${e.message}`, 'error');
            } finally {
                if(btnEvent) btnEvent.target.classList.remove('is-loading');
            }
        },

        async setupCommunityList(useCommunityList, btnEvent) {
            if(btnEvent) btnEvent.target.classList.add('is-loading');
            try {
                if (!this.settings) this.settings = await fetchSettings();
                if (!this.settings.IgnoredUpdateOptions) this.settings.IgnoredUpdateOptions = {};
                this.settings.IgnoredUpdateOptions.UseCommunityList = useCommunityList;
                await saveSettings(this.settings);
                this.ui.showCommunityListModal = false;
                this.showToast('Community list preference saved.', 'success');
                if (useCommunityList) {
                    this.handleScan();
                }
            } catch (e) {
                this.showToast(`Error saving settings: ${e.message}`, 'error');
            } finally {
                if(btnEvent) btnEvent.target.classList.remove('is-loading');
            }
        },

        closeModals() {
            this.ui.showIgnoreModal = false;
            this.ui.showSettingsModal = false;
            this.ui.showCommunityListModal = false;
            if (this.ui.lastFocus) {
                this.ui.lastFocus.focus();
                this.ui.lastFocus = null;
            }
        },

        showToast(message, type = 'info') {
            this.ui.toastIdCounter++;
            this.ui.toasts.push({ id: this.ui.toastIdCounter, message, type, show: true });
        },

        formatTimeAgo() {
            if (!this.meta.lastScan) return 'Never';
            const seconds = Math.floor((this.ui.currentTime - this.meta.lastScan) / 1000);
            if (seconds > 59) return `${Math.floor(seconds / 60)}m ago`;
            if (seconds > 10) return `${seconds}s ago`;
            return 'Just now';
        },

        getStatusClass(mod) {
            if (mod.isIgnored) return 'status-unknown';
            if (mod.status === 'UpToDate') return 'status-ok';
            if (mod.status === 'NewerInstalled') return 'status-newer';
            if (mod.status === 'UpdateAvailable') return 'status-update';
            if (['UpdateBlocked', 'Incompatible', 'Error', 'NoVersionsFound'].includes(mod.status)) return 'status-blocked';
            return 'status-unknown';
        },

        getPillClass(mod) {
            if (mod.isIgnored) return 'status-pill-unknown';
            if (mod.status === 'UpToDate') return 'status-pill-ok';
            if (mod.status === 'NewerInstalled') return 'status-pill-newer';
            if (mod.status === 'UpdateAvailable') return 'status-pill-update';
            if (['UpdateBlocked', 'Incompatible', 'Error', 'NoVersionsFound'].includes(mod.status)) return 'status-pill-blocked';
            return 'status-pill-unknown';
        },

        getPillText(mod) {
            if (mod.isIgnored) return mod.ignoreSource === 'User' ? 'IGNORED (YOU)' : (mod.ignoreSource === 'Remote' ? 'IGNORED (COMMUNITY)' : 'IGNORED');
            if (mod.status === 'UpToDate') return 'UP TO DATE';
            if (mod.status === 'NewerInstalled') return 'NEWER';
            if (mod.status === 'UpdateAvailable') return 'UPDATE';
            if (mod.status === 'UpdateBlocked') return 'BLOCKED';
            if (mod.status === 'Incompatible') return 'INCOMPATIBLE';
            return 'UNKNOWN';
        },

        handleKeydown(e) {
            // Why: Enables keyboard navigation (Vim-style j/k or arrows)
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.tagName === 'SELECT') return;

            const modalOpen = this.ui.showIgnoreModal || this.ui.showSettingsModal;
            if (e.key === 'Escape') {
                if (modalOpen) {
                    this.closeModals();
                    return;
                }
                this.selectedMod = null;
            } else if (!modalOpen && (e.key === 'j' || e.key === 'k' || e.key === 'ArrowDown' || e.key === 'ArrowUp')) {
                if (this.filteredMods.length === 0) return;
                e.preventDefault();
                let idx = -1;
                if (this.selectedMod) {
                    idx = this.filteredMods.findIndex(m => m.id === this.selectedMod.id);
                }
                
                if (idx === -1) {
                    idx = 0;
                } else {
                    if (e.key === 'j' || e.key === 'ArrowDown') idx = Math.min(idx + 1, this.filteredMods.length - 1);
                    if (e.key === 'k' || e.key === 'ArrowUp') idx = Math.max(idx - 1, 0);
                }
                this.selectedMod = this.filteredMods[idx];
            }
        }
    }));
});
