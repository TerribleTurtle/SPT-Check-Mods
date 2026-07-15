using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Web;
using OneOf;

namespace CheckModsExtended.Tests.Fakes;

public class FakeSettingsService : ISettingsService
{
    public Task<string> GetSettingsAsync(CancellationToken token = default) => Task.FromResult("{}");
    public Task<OneOf<MessageResponse, ApiError>> UpdateSettingsAsync(string jsonPayload, CancellationToken token = default) => Task.FromResult<OneOf<MessageResponse, ApiError>>(new MessageResponse("OK"));
    public Task UpdateIgnoredUpdateOptionsAsync(bool useCommunityList, CancellationToken token = default) => Task.CompletedTask;
}
