using StuffInABox.Domain.Entities;

namespace StuffInABox.Domain.Tests.Entities;

public class UserSettingsTests
{
    [Fact]
    public void RecordAiUsage_CountsWithinTheSameMonth()
    {
        var s = UserSettings.CreateDefault(Guid.NewGuid());

        s.RecordAiUsage(202607);
        s.RecordAiUsage(202607);

        Assert.Equal(2, s.AiUsedIn(202607));
    }

    [Fact]
    public void RecordAiUsage_ResetsWhenMonthRollsOver()
    {
        var s = UserSettings.CreateDefault(Guid.NewGuid());
        s.RecordAiUsage(202606);
        s.RecordAiUsage(202606);

        s.RecordAiUsage(202607); // new month → counter restarts

        Assert.Equal(1, s.AiUsedIn(202607));
        Assert.Equal(0, s.AiUsedIn(202606)); // an old month reads as zero
    }
}
