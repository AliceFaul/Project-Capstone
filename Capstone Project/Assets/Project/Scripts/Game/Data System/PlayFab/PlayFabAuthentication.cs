using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

[CreateAssetMenu(fileName = "PlayFabAuthentication", menuName = "PlayFab/Authentication")]
public class PlayFabAuthentication : PlayFabService
{
    public override async Task<bool> Initialize(IServiceRegistry serviceRegistry, CancellationToken ct)
    {
        serviceRegistry.Register<PlayFabAuthentication>(this);
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> DefaultIdLogin(CancellationToken ct = default)
    {
        var task = new TaskCompletionSource<bool>();

        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };
        
        PlayFabClientAPI.LoginWithCustomID(request, result =>
        {
            Debug.Log($"[PlayFabAuthentication] Custom id login successfully. Id: {result.PlayFabId}");
            task.TrySetResult(true);
        },
        error =>
        {
            Debug.LogError($"[PlayFabAuthentication] Custom id login failed. {error.GenerateErrorReport()}");
            task.TrySetResult(false);
        });
        
        return await AsyncUtils.WaitWithCancellation(task.Task, ct);
    }

    public async Task<bool> GoogleLogin(string token, CancellationToken ct = default)
    {
        var task = new TaskCompletionSource<bool>();

        var request = new LoginWithGoogleAccountRequest
        {
            TitleId = PlayFabSettings.TitleId,
            ServerAuthCode = token,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithGoogleAccount(request, result =>
        {
            Debug.Log($"[PlayFabAuthentication] Google login successfully. Id: {result.PlayFabId}");
            task.TrySetResult(true);
        },
        error =>
        {
            Debug.LogError($"[PlayFabAuthentication] Google login failed. {error.GenerateErrorReport()}");
            task.TrySetResult(false);
        });
        
        return await AsyncUtils.WaitWithCancellation(task.Task, ct);
    }

    public async Task<bool> FacebookLogin(string token, CancellationToken ct = default)
    {
        var task = new TaskCompletionSource<bool>();

        var request = new LoginWithFacebookRequest
        {
            TitleId = PlayFabSettings.TitleId,
            AccessToken = token,
            CreateAccount = true
        };
        
        PlayFabClientAPI.LoginWithFacebook(request, result =>
        {
            Debug.Log($"[PlayFabAuthentication] Facebook login successfully. Id: {result.PlayFabId}");
            task.TrySetResult(true);
        },
        error =>
        {
            Debug.LogError($"[PlayFabAuthentication] Facebook login failed. {error.GenerateErrorReport()}");
            task.TrySetResult(false);
        });
        
        return await AsyncUtils.WaitWithCancellation(task.Task, ct);
    }

    public async Task<bool> EmailRegister(string email, string password, string userName, CancellationToken ct = default)
    {
        var task = new TaskCompletionSource<bool>();

        var request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            Username = userName,
            RequireBothUsernameAndEmail = true
        };
        
        PlayFabClientAPI.RegisterPlayFabUser(request, result =>
        {
            Debug.Log($"[PlayFabAuthentication] Email register successfully. Id: {result.PlayFabId}");
            task.TrySetResult(true);
        },
        error =>
        {
            Debug.LogError($"[PlayFabAuthentication] Email register failed. {error.GenerateErrorReport()}");
            task.TrySetResult(false);
        });
        
        return await AsyncUtils.WaitWithCancellation(task.Task, ct);
    }

    public async Task<bool> EmailLogin(string email, string password, CancellationToken ct = default)
    {
        var task = new TaskCompletionSource<bool>();

        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password
        };
        
        PlayFabClientAPI.LoginWithEmailAddress(request, result =>
        {
            Debug.Log($"[PlayFabAuthentication] Email login successfully. Id: {result.PlayFabId}");
            task.TrySetResult(true);
        },
        error =>
        {
            Debug.LogError($"[PlayFabAuthentication] Email login failed. {error.GenerateErrorReport()}");
            task.TrySetResult(false);
        });
        
        return await AsyncUtils.WaitWithCancellation(task.Task, ct);
    }

    public async Task<bool> RecoveryPassword(string email, CancellationToken ct = default)
    {
        var task = new TaskCompletionSource<bool>();

        var request = new SendAccountRecoveryEmailRequest
        {
            Email = email,
            TitleId = PlayFabSettings.TitleId
        };
        
        PlayFabClientAPI.SendAccountRecoveryEmail(request, result =>
        {
            Debug.Log($"[PlayFabAuthentication] Recovery email sent.");
            task.TrySetResult(true);
        },
        error =>
        {
            Debug.LogError($"[PlayFabAuthentication] Failed to send recovery email. {error.GenerateErrorReport()}");
            task.TrySetResult(false);
        });
        
        return await AsyncUtils.WaitWithCancellation(task.Task, ct);
    }
}