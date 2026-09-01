namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class PlusResultTests
{
    [Fact]
    public void Success_Has_No_Code()
    {
        Assert.True(PlusResult.Success.Succeeded);
        Assert.Null(PlusResult.Success.Code);
    }

    [Fact]
    public void Fail_And_Unsupported_Set_Stable_Codes()
    {
        var fail = PlusResult.Fail(PlusErrorCodes.Denied, "No");
        var unsupported = PlusResult.Unsupported("Wallet");

        Assert.False(fail.Succeeded);
        Assert.Equal(PlusErrorCodes.Denied, fail.Code);
        Assert.Equal(PlusErrorCodes.Unsupported, unsupported.Code);
        Assert.Contains("Wallet", unsupported.Message);
    }

    [Fact]
    public void Generic_Ok_And_Fail_Preserve_Value()
    {
        var ok = PlusResult<int>.Ok(7);
        var fail = PlusResult<int>.Fail(PlusErrorCodes.TransientFailure, "down");

        Assert.True(ok.Succeeded);
        Assert.Equal(7, ok.Value);
        Assert.False(fail.Succeeded);
        Assert.Equal(0, fail.Value);
        Assert.Equal(PlusErrorCodes.TransientFailure, fail.Code);
    }
}
