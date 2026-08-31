using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkitPlus.Sample;

static class SampleServices
{
    public static T? Get<T>() where T : class =>
        IPlatformApplication.Current?.Services.GetService<T>();
}
