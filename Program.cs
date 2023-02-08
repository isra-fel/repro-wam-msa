using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

internal class Program
{
    //private const string ClientId = "04f0c124-f2bc-4f59-8241-bf6df9866bbd"; // new VS
    private const string ClientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46"; // cli
    //private const string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2"; // powershell
    public static readonly string UserCacheFile =
            System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.user.json";

    private static async Task Main(string[] args)
    {
        var brokerOptions = new WindowsBrokerOptions
        {
            //ListWindowsWorkAndSchoolAccounts = false,
            MsaPassthrough = true,
        };

        var pca = PublicClientApplicationBuilder.Create(ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "organizations")
            .WithWindowsBrokerOptions(brokerOptions)
            //.WithRedirectUri("http://localhost")
            .WithBrokerPreview(true)
            .WithParentActivityOrWindow(() => GetConsoleOrTerminalWindow())
            .WithLogging((x, y, z) => Console.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
            .Build();
        BindCache(pca.UserTokenCache, UserCacheFile);

        var result = await pca.AcquireTokenInteractive(new[] { "https://management.core.windows.net//.default" }).ExecuteAsync().ConfigureAwait(false);



        pca = PublicClientApplicationBuilder.Create(ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "5bc0604d-40a5-4aa7-894a-a538fb85dcda")
            .WithWindowsBrokerOptions(brokerOptions)
            .WithBrokerPreview(true)
            .WithLogging((x, y, z) => Console.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
            .Build();
        BindCache(pca.UserTokenCache, UserCacheFile);

        var accounts = await pca.GetAccountsAsync();
        var account = accounts.FirstOrDefault();
        result = await pca.AcquireTokenSilent(new[] { "https://management.core.windows.net//.default" }, account).ExecuteAsync().ConfigureAwait(false);

        Debugger.Break();
    }

    private static void BindCache(ITokenCache tokenCache, string file)
    {
        tokenCache.SetBeforeAccess(notificationArgs =>
        {
            notificationArgs.TokenCache.DeserializeMsalV3(File.Exists(file)
                ? File.ReadAllBytes(UserCacheFile)
                : null);
        });

        tokenCache.SetAfterAccess(notificationArgs =>
        {
            // if the access operation resulted in a cache update
            if (notificationArgs.HasStateChanged)
            {
                // reflect changes in the persistent store
                File.WriteAllBytes(file, notificationArgs.TokenCache.SerializeMsalV3());
            }
        });
    }
    private static void ClearCache(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(null);
        tokenCache.SetAfterAccess(null);
    }

    // sign in with current user
    //private static async Task Main(string[] args)
    //{
    //    var brokerOptions = new WindowsBrokerOptions
    //    {
    //        ListWindowsWorkAndSchoolAccounts = false,
    //        // MsaPassthrough = true,
    //    };

    //    var publicClientBuilder = PublicClientApplicationBuilder.Create("04b07795-8ddb-461a-bbee-02f9e1bf7b46")
    //    // var publicClientBuilder = PublicClientApplicationBuilder.Create("1950a258-227b-4e31-a9cf-717495945fc2")
    //        .WithAuthority(AzureCloudInstance.AzurePublic, "organizations")
    //        .WithWindowsBrokerOptions(brokerOptions)
    //        .WithBrokerPreview()
    //        .WithParentActivityOrWindow(() => GetConsoleOrTerminalWindow());

    //    var publicClient = publicClientBuilder.Build();

    //    var result = await publicClient.AcquireTokenSilent(new[] { "https://management.core.windows.net//.default" }, PublicClientApplication.OperatingSystemAccount).ExecuteAsync();
    //    Console.WriteLine(result.AccessToken);
    //}
    /// <summary>
    /// Retrieves the handle to the ancestor of the specified window.
    /// </summary>
    /// <param name="hwnd">A handle to the window whose ancestor is to be retrieved.
    /// If this parameter is the desktop window, the function returns NULL. </param>
    /// <param name="flags">The ancestor to be retrieved.</param>
    /// <returns>The return value is the handle to the ancestor window.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    static IntPtr GetConsoleOrTerminalWindow()
    {
        IntPtr consoleHandle = GetConsoleWindow();
        IntPtr handle = GetAncestor(consoleHandle, GetAncestorFlags.GetRootOwner);

        return handle;
    }
}

enum GetAncestorFlags
{
    GetParent = 1,
    GetRoot = 2,
    /// <summary>
    /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
    /// </summary>
    GetRootOwner = 3
}