using Cloris.Aion2Flow.Services.Settings;

namespace Cloris.Aion2Flow.Tests.App;

public sealed class LiteModeSettingsTests
{
    [Fact]
    public void LiteMode_DefaultsToOn_AndRoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), "Aion2Flow.Tests", $"{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(path);
            Assert.True(service.Current.LiteMode);

            service.Update(static s => s.LiteMode = false);
            Assert.False(new SettingsService(path).Current.LiteMode);

            service.Update(static s => s.LiteMode = true);
            Assert.True(new SettingsService(path).Current.LiteMode);
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".tmp");
        }
    }
}
