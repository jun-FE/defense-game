// STEAMWORKS_NET은 Steamworks.NET 패키지가 설치돼 있을 때만 정의된다(DefenseGame.Steam.asmdef).
// 패키지가 없어도 나머지 게임 코드는 컴파일된다.
#if !STEAMWORKS_NET || !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// Steam API 초기화/종료. 게임 시작 시 자동으로 생성된다.
/// Steam이 안 켜져 있어도 게임은 그대로 실행되고, Initialized만 false가 된다.
/// </summary>
public class SteamManager : MonoBehaviour
{
    /// <summary>Steamworks에서 받은 App ID로 바꾼다. 480은 Valve 테스트용(Spacewar).</summary>
    public const uint AppId = 480;

    public static bool Initialized { get; private set; }

    static SteamManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject(nameof(SteamManager));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SteamManager>();
    }

#if !DISABLESTEAMWORKS
    void Awake()
    {
        if (!Packsize.Test())
        {
            Debug.LogError("[Steam] Steamworks.NET 패키지 플랫폼 설정이 맞지 않습니다.");
            return;
        }

        try
        {
#if !UNITY_EDITOR
            // Steam 밖에서 실행하면 Steam을 통해 다시 실행시킨다(출시 빌드에서는 steam_appid.txt를 빼야 동작).
            if (SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
            {
                Application.Quit();
                return;
            }
#endif
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError("[Steam] steam_api64.dll을 찾을 수 없습니다.\n" + e);
            return;
        }

        ESteamAPIInitResult result = SteamAPI.InitEx(out string error);
        Initialized = result == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
        if (Initialized)
            Debug.Log($"[Steam] 초기화 성공: {SteamFriends.GetPersonaName()}");
        else
            Debug.LogWarning($"[Steam] 초기화 실패({result}): {error} - Steam 클라이언트가 실행 중인지 확인하세요.");
    }

    void Update()
    {
        if (Initialized) SteamAPI.RunCallbacks();
    }

    void OnApplicationQuit()
    {
        if (!Initialized) return;
        SteamAPI.Shutdown();
        Initialized = false;
    }
#endif
}
