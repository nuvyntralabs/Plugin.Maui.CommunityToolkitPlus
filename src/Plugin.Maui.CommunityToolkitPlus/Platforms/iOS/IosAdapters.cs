#if IOS
using DeviceCheck;
using Foundation;
using PassKit;
using UIKit;

namespace Plugin.Maui.CommunityToolkitPlus;

sealed class IosIntegrityAdapter : IIntegrityPlatformAdapter
{
    readonly IPlusStore _store;

    public IosIntegrityAdapter(IPlusStore store) => _store = store;

    public IntegrityCapability GetCapability()
    {
        var supported = DCAppAttestService.SharedService.Supported;
        return new(supported, supported, supported, "ios");
    }

    public async Task<IntegrityOperationResult> CreateProofAsync(
        IntegrityChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        var service = DCAppAttestService.SharedService;
        if (!service.Supported)
        {
            return IntegrityOperationResult.Fail(
                IntegrityErrorCodes.Unsupported,
                "App Attest is not available on this device.");
        }

        var record = await _store.LoadAsync<IntegrityKeyRecord>("app-integrity-key", cancellationToken)
            .ConfigureAwait(false);
        string keyId;
        try
        {
            if (string.IsNullOrWhiteSpace(record?.KeyId))
            {
                keyId = await service.GenerateKeyAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                record = new IntegrityKeyRecord
                {
                    KeyId = keyId,
                    Platform = "ios"
                };
                await _store.SaveAsync("app-integrity-key", record, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                keyId = record.KeyId;
            }

            var clientData = NSData.FromString(challenge.Nonce, NSStringEncoding.UTF8);
            var hash = new byte[32];
            using (var sha = System.Security.Cryptography.SHA256.Create())
                hash = sha.ComputeHash(clientData.ToArray());

            var clientHash = NSData.FromArray(hash);
            NSData assertion;
            try
            {
                assertion = await service.GenerateAssertionAsync(keyId, clientHash)
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                var attestation = await service.AttestKeyAsync(keyId, clientHash)
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
                assertion = attestation;
            }

            return IntegrityOperationResult.Ok(new IntegrityProof(
                challenge.Id,
                "ios",
                assertion.GetBase64EncodedString(NSDataBase64EncodingOptions.None),
                record.KeyId));
        }
        catch (OperationCanceledException)
        {
            return IntegrityOperationResult.Fail(IntegrityErrorCodes.Cancelled, "App Attest was cancelled.");
        }
        catch (Exception)
        {
            await _store.DeleteAsync("app-integrity-key", cancellationToken).ConfigureAwait(false);
            return IntegrityOperationResult.Fail(
                IntegrityErrorCodes.KeyLost,
                "The App Attest key is missing or invalid and must be regenerated.");
        }
    }
}

sealed class IosWalletAdapter : IWalletPlatformAdapter
{
    public WalletCapability GetCapability() =>
        new(PKAddPassesViewController.CanAddPasses, true, false, false, "ios");

    public Task<WalletOperationResult> AddAsync(
        WalletPassPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (payload.PkPass is null || payload.PkPass.Length == 0)
        {
            return Task.FromResult(WalletOperationResult.Fail(
                WalletErrorCodes.InvalidPayload,
                "iOS wallet handoff requires backend-issued .pkpass bytes."));
        }

        if (!PKAddPassesViewController.CanAddPasses)
        {
            return Task.FromResult(WalletOperationResult.Fail(
                WalletErrorCodes.Unsupported,
                "This iOS device cannot add Wallet passes."));
        }

        NSError? error;
        var pass = new PKPass(NSData.FromArray(payload.PkPass), out error);
        if (error is not null || pass is null)
        {
            return Task.FromResult(WalletOperationResult.Fail(
                WalletErrorCodes.InvalidPayload,
                "The supplied .pkpass payload is not a valid Apple Wallet pass."));
        }

        var presenter = GetPresenter();
        if (presenter is null)
        {
            return Task.FromResult(WalletOperationResult.Fail(
                PlusErrorCodes.InvalidState,
                "No iOS view controller is available to present Wallet."));
        }

        var tcs = new TaskCompletionSource<WalletOperationResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var controller = new PKAddPassesViewController(pass)
        {
            Delegate = new AddPassesCompletion(tcs)
        };

        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() =>
            {
                controller.DismissViewController(true, null);
                tcs.TrySetCanceled(cancellationToken);
            });
        }

        presenter.PresentViewController(controller, true, null);
        return AwaitAddAsync(tcs.Task, registration);
    }

    static async Task<WalletOperationResult> AwaitAddAsync(
        Task<WalletOperationResult> presented,
        CancellationTokenRegistration registration)
    {
        try
        {
            return await presented.ConfigureAwait(false);
        }
        finally
        {
            await registration.DisposeAsync().ConfigureAwait(false);
        }
    }

    static UIViewController? GetPresenter()
    {
        var window = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .SelectMany(scene => scene.Windows)
            .FirstOrDefault(item => item.IsKeyWindow);
        return window?.RootViewController;
    }

    sealed class AddPassesCompletion(TaskCompletionSource<WalletOperationResult> completed)
        : PKAddPassesViewControllerDelegate
    {
        public override void Finished(PKAddPassesViewController controller)
        {
            controller.DismissViewController(true, () =>
                completed.TrySetResult(WalletOperationResult.Ok()));
        }
    }
}

sealed class IosAttConsentAdapter : IConsentPlatformAdapter
{
    public string Name => "att";

    public async Task<ConsentDecision?> RequestAsync(CancellationToken cancellationToken = default)
    {
        var usage = NSBundle.MainBundle.ObjectForInfoDictionary("NSUserTrackingUsageDescription");
        if (usage is null)
            return null;

        var status = await AppTrackingTransparency.ATTrackingManager
            .RequestTrackingAuthorizationAsync()
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        return status == AppTrackingTransparency.ATTrackingManagerAuthorizationStatus.Authorized
            ? ConsentDecision.Accepted
            : ConsentDecision.Denied;
    }
}
#endif
