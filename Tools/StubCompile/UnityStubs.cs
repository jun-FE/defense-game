// Minimal fake UnityEngine/UnityEditor API for compile-checking only.
using System;
using System.Collections;
using System.Collections.Generic;
namespace UnityEngine {
  public class Object { public string name; public HideFlags hideFlags;
    public static void Destroy(Object o){} public static void DestroyImmediate(Object o){} public static void DontDestroyOnLoad(Object o){}
    public static T Instantiate<T>(T o) where T:Object=>o; public static T Instantiate<T>(T o, Vector3 p, Quaternion q) where T:Object=>o; public static T Instantiate<T>(T o, Transform parent) where T:Object=>o;
    public static implicit operator bool(Object o)=>!ReferenceEquals(o,null); }
  public class Component : Object { public Transform transform; public GameObject gameObject; public T GetComponent<T>()=>default(T); public T GetComponentInChildren<T>()=>default(T); }
  public class Behaviour : Component { public bool enabled; public bool isActiveAndEnabled; }
  public class MonoBehaviour : Behaviour { public Coroutine StartCoroutine(IEnumerator e)=>null; }
  public class ScriptableObject : Object { public static T CreateInstance<T>() where T:ScriptableObject=>default(T); }
  public class Coroutine {} public class WaitForSeconds { public WaitForSeconds(float f){} }
  public class GameObject : Object { public string tag; public Transform transform; public GameObject(){} public GameObject(string n){} public T AddComponent<T>() where T:Component=>default(T); public T GetComponent<T>()=>default(T); public void SetActive(bool b){} }
  public class Transform : Component { public Vector3 TransformPoint(Vector3 p)=>p; public void Rotate(float x,float y,float z){} public Vector3 position, localPosition, localScale; public Quaternion rotation; public Transform parent; public void SetParent(Transform t){} public void SetParent(Transform t,bool b){} public Transform Find(string n)=>null; }
  public struct Vector2 { public Vector2 normalized=>this; public static Vector2 Min(Vector2 a,Vector2 b)=>a; public static Vector2 Max(Vector2 a,Vector2 b)=>a; public float sqrMagnitude=>0; public float x,y; public Vector2(float a,float b){x=a;y=b;} public static Vector2 one, zero, down, up, left, right;
    public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a; public static float Distance(Vector2 a,Vector2 b)=>0;
    public static Vector2 operator-(Vector2 a,Vector2 b)=>a; public static Vector2 operator-(Vector2 a)=>a; public static Vector2 operator+(Vector2 a,Vector2 b)=>a; public static Vector2 operator*(Vector2 a,float b)=>a; public static Vector2 operator/(Vector2 a,float b)=>a;
    public static implicit operator Vector3(Vector2 v)=>default(Vector3); public static implicit operator Vector2(Vector3 v)=>default(Vector2); }
  public struct Vector2Int { public static Vector2Int zero; public int x,y; public Vector2Int(int a,int b){x=a;y=b;} public static bool operator==(Vector2Int a,Vector2Int b)=>a.x==b.x&&a.y==b.y; public static bool operator!=(Vector2Int a,Vector2Int b)=>!(a==b); public override bool Equals(object o)=>o is Vector2Int v&&v==this; public override int GetHashCode()=>x*31+y; }
  public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} public static Vector3 one, zero, up; public static Vector3 operator /(Vector3 a,float b)=>a; public static float Distance(Vector3 a,Vector3 b)=>0; public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a; public float sqrMagnitude=>0;
    public static Vector3 MoveTowards(Vector3 a,Vector3 b,float d)=>a; public static Vector3 operator-(Vector3 a,Vector3 b)=>a; public static Vector3 operator+(Vector3 a,Vector3 b)=>a; public static Vector3 operator*(Vector3 a,float b)=>a; public static Vector3 operator*(float b,Vector3 a)=>a; }
  public struct Quaternion { public static Quaternion identity; public static Quaternion Euler(float a,float b,float c)=>default(Quaternion); }
  public struct Color { public float r,g,b,a; public static Color operator *(Color a, Color b)=>a; public Color(float r,float g,float b){this.r=r;this.g=g;this.b=b;a=1;} public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}
    public static Color white, cyan, yellow, black; public static Color Lerp(Color a,Color b,float t)=>a; public static Color operator*(Color a,float b)=>a; public static Color operator+(Color a,Color b)=>a; }
  public struct Rect { public Vector2 position { get { return default(Vector2); } set {} } public Vector2 center=>default(Vector2); public bool Contains(Vector2 p)=>true; public float x,y,width,height; public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;} public float xMin=>x; public float yMin=>y; public float xMax=>x+width; public float yMax=>y+height; public static Rect MinMaxRect(float a,float b,float c,float d)=>default(Rect); }
  public struct Matrix4x4 { public static Matrix4x4 identity; public static Matrix4x4 Scale(Vector3 v)=>default(Matrix4x4); }
  public static class Mathf { public const float PI=3.14159f; public const float Deg2Rad=0.0174f; public static float Cos(float f)=>f; public static float SmoothStep(float a,float b,float t)=>a;  public const float Rad2Deg=57f; public static float Max(float a,float b)=>a; public static int Max(int a,int b)=>a; public static float Min(float a,float b)=>a; public static int Min(int a,int b)=>a;
    public static float Clamp01(float a)=>a; public static float Clamp(float v,float a,float b)=>v; public static int Clamp(int v,int a,int b)=>v; public static float Abs(float a)=>a; public static float Atan2(float a,float b)=>a; public static float Pow(float a,float b)=>a; public static float Sign(float a)=>a; public static float PerlinNoise(float a,float b)=>a; public static float Sqrt(float a)=>a; public static float InverseLerp(float a,float b,float v)=>v; public static float Lerp(float a,float b,float t)=>a;
    public static int FloorToInt(float a)=>0; public static float Exp(float a)=>a; public static float Sin(float a)=>a; public static int CeilToInt(float a)=>0; public static int RoundToInt(float a)=>0; public static bool Approximately(float a,float b)=>true; }
  public static class Time { public static float deltaTime, timeScale, unscaledTime, time, unscaledDeltaTime; }
  public static class Screen { public static int width, height; public static bool fullScreen; public static Resolution[] resolutions; public static void SetResolution(int w,int h,FullScreenMode m){} }
  public struct Resolution { public int width, height; }
  public enum FullScreenMode { FullScreenWindow, Windowed }
  public static class Debug { public static bool isDebugBuild; public static void Log(object o){} public static void LogWarning(object o){} public static void LogError(object o){} public static void LogError(object o, Object c){} }
  public static class Gizmos { public static Color color; }
  public static class Application { public static string version, dataPath, persistentDataPath; public static bool isEditor; public static void Quit(){} }
  public static class Random { public static float value; public static Vector2 insideUnitCircle; public static float Range(float a,float b)=>a; public static int Range(int a,int b)=>a; }
  public static class PlayerPrefs { public static string GetString(string k,string d)=>d; public static void SetString(string k,string v){} public static int GetInt(string k,int d)=>d; public static void SetInt(string k,int v){} public static float GetFloat(string k,float d)=>d; public static void SetFloat(string k,float v){} public static void DeleteKey(string k){} public static void Save(){} }
  public static class Resources { public static T[] LoadAll<T>(string p) where T:Object=>null; public static T Load<T>(string p) where T:Object=>default(T); }
  public class Camera : Behaviour { public Vector3 WorldToScreenPoint(Vector3 p)=>p; public float aspect; public static Camera main; public bool orthographic; public float orthographicSize; public CameraClearFlags clearFlags; public Color backgroundColor; public Vector3 ScreenToWorldPoint(Vector3 v)=>v; }
  public enum CameraClearFlags { SolidColor }
  public class AudioListener : Behaviour { public static float volume; }
  public class SpriteRenderer : Component { public Sprite sprite; public Color color; public int sortingOrder; public bool enabled, flipX; public Material sharedMaterial; public SpriteDrawMode drawMode; public Vector2 size; }
  public enum SpriteDrawMode { Simple, Sliced, Tiled }
  public enum SpriteMeshType { FullRect, Tight }
  public class Sprite : Object { public Rect textureRect; public Texture2D texture; public Rect rect; public float pixelsPerUnit; public static Sprite Create(Texture2D t,Rect r,Vector2 p,float ppu)=>null; public static Sprite Create(Texture2D t,Rect r,Vector2 p,float ppu,uint e,SpriteMeshType m)=>null; public Bounds bounds; }
  public struct Bounds { public Vector3 size; }
  public enum FilterMode { Point, Bilinear, Trilinear }
  public class Texture2D : Object { public int width, height; public FilterMode filterMode; public void SetPixels(Color[] c){} public static Texture2D whiteTexture; public Texture2D(int w,int h){} public Texture2D(int w,int h,TextureFormat f,bool m){} public void SetPixel(int x,int y,Color c){} public void Apply(){} public TextureWrapMode wrapMode; public void SetPixels32(Color32[] c){} }
  public enum TextureWrapMode { Clamp, Repeat } public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} }
  public class Shader : Object { public static Shader Find(string n)=>null; }
  public enum TextureFormat { RGBA32 } public enum HideFlags { HideAndDontSave }
  public static class ImageConversion { public static byte[] EncodeToPNG(this Texture2D t)=>null; }
  public class HeaderAttribute : Attribute { public HeaderAttribute(string s){} }
  public class TooltipAttribute : Attribute { public TooltipAttribute(string s){} }
  public class RangeAttribute : Attribute { public RangeAttribute(float a,float b){} }
  public class TextAreaAttribute : Attribute { public TextAreaAttribute(int a,int b){} }
  public class CreateAssetMenuAttribute : Attribute { public string menuName, fileName; }
  public class RequireComponent : Attribute { public RequireComponent(Type t){} }
  public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){} }
  public enum RuntimeInitializeLoadType { BeforeSceneLoad, AfterSceneLoad }
  public class GUIStyle { public GUIStyleState focused = new GUIStyleState(); public static GUIStyle none; public RectOffset border; public RectOffset padding; public void Draw(Rect r,bool a,bool b,bool c,bool d){} public float CalcHeight(GUIContent c,float w)=>0; public Vector2 CalcSize(GUIContent c)=>default(Vector2); public GUIStyle(){} public GUIStyle(GUIStyle o){} public int fontSize; public FontStyle fontStyle; public TextAnchor alignment; public GUIStyleState normal, hover, active; public bool wordWrap, richText; public ImagePosition imagePosition; }
  public class GUIStyleState { public Color textColor; public Texture2D background; }
  public enum FontStyle { Bold, Normal } public enum TextAnchor { LowerCenter, UpperCenter, MiddleCenter, MiddleLeft, MiddleRight, UpperLeft } public enum ImagePosition { ImageLeft }
  public class GUISkin { public GUIStyle label, button, box; }
  public class GUIContent { public string text; public static GUIContent none; public GUIContent(){} public GUIContent(string t){} public GUIContent(string t, Texture2D i){} }
  public static class GUI { public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv){} public static void BeginClip(Rect r){} public static void EndClip(){} public static Vector2 BeginScrollView(Rect a,Vector2 b,Rect c)=>b; public static void EndScrollView(){} public static string TextField(Rect r,string t)=>t; public static bool Toggle(Rect r,bool v,string t)=>v; public static Matrix4x4 matrix; public static bool enabled; public static GUISkin skin; public static Color color;
    public static bool Button(Rect r,string s)=>false; public static bool Button(Rect r,string s,GUIStyle st)=>false; public static bool Button(Rect r,GUIContent c,GUIStyle st)=>false;
    public static void Label(Rect r,string s,GUIStyle st){} public static void Box(Rect r,GUIContent c){} public static void Box(Rect r,GUIContent c,GUIStyle s){} public static void DrawTexture(Rect r,Texture2D t){} public static void DrawTexture(Rect r,Texture2D t,ScaleMode m){}
    public static float HorizontalSlider(Rect r,float v,float a,float b)=>v; }
  public class GUILayoutOption {}
  public static class GUILayout { public static void BeginArea(Rect r){} public static void EndArea(){} public static void BeginHorizontal(){} public static void EndHorizontal(){} public static void FlexibleSpace(){} public static void Label(string s,GUIStyle st,params GUILayoutOption[] o){} public static bool Button(string s,GUIStyle st,params GUILayoutOption[] o)=>false; public static GUILayoutOption Width(float w)=>null; }
  public class Event { public static Event current; public EventType type; public KeyCode keyCode; public int button; public Vector2 mousePosition, delta; public bool alt, control, command; public void Use(){} }
  public static class GUIUtility { public static int keyboardControl; public static void RotateAroundPivot(float a, Vector2 p){} }
  public static class Input { public static Vector2 mouseScrollDelta; public static Vector3 mousePosition; public static bool GetMouseButtonDown(int b)=>false; public static bool GetMouseButton(int b)=>false; public static bool GetMouseButtonUp(int b)=>false; public static bool GetKey(KeyCode k)=>false; public static bool GetKeyDown(KeyCode k)=>false; }
  public enum EventType { KeyDown, MouseDown, MouseMove, MouseDrag, Repaint, MouseUp, ScrollWheel }
  public enum KeyCode { W, A, D, Space, Alpha1, Alpha6, Alpha7, Alpha9, B, F, P, S, Z, F12, LeftArrow, RightArrow, Escape, Return, KeypadEnter, F1, F2, UpArrow, DownArrow }
}
namespace UnityEngine.SceneManagement { public struct Scene { public int buildIndex; public string name; } public static class SceneManager { public static Scene GetActiveScene()=>default(Scene); public static void LoadScene(int i){} public static void LoadScene(string s){} } }
namespace UnityEngine.UIElements {}
namespace UnityEditor {
  using UnityEngine;
  public class MenuItem : Attribute { public MenuItem(string s){} }
  public class InitializeOnLoadAttribute : Attribute {}
  public static class EditorPrefs { public static bool GetBool(string k,bool d)=>d; public static void SetBool(string k,bool v){} }
  public static class AssetDatabase { public static string GetAssetPath(Object o)=>""; public static void Refresh(){} public static bool IsValidFolder(string p)=>true; public static string CreateFolder(string a,string b)=>""; public static void ImportAsset(string p){} public static void SaveAssets(){}
    public static T LoadAssetAtPath<T>(string p)=>default(T); public static void CreateAsset(Object o,string p){} public static bool DeleteAsset(string p)=>true; public static string[] FindAssets(string f)=>null; public static string[] FindAssets(string f,string[] d)=>null; public static string GUIDToAssetPath(string g)=>null; }
  public class AssetImporter : Object { public string assetPath; public static AssetImporter GetAtPath(string p)=>null; public void SaveAndReimport(){} }
  public class AssetPostprocessor { public string assetPath; public AssetImporter assetImporter; }
  public class TextureImporterSettings { public SpriteMeshType spriteMeshType; }
  public class TextureImporter : AssetImporter { public TextureWrapMode wrapMode; public void ReadTextureSettings(TextureImporterSettings s){} public void SetTextureSettings(TextureImporterSettings s){} public TextureImporterType textureType; public SpriteImportMode spriteImportMode; public float spritePixelsPerUnit; public int maxTextureSize; public bool mipmapEnabled, alphaIsTransparency; public TextureImporterCompression textureCompression; }
  public enum TextureImporterType { Sprite } public enum SpriteImportMode { Single } public enum TextureImporterCompression { Uncompressed, CompressedHQ }
  public static class PrefabUtility { public static GameObject SaveAsPrefabAsset(GameObject g,string p)=>g; }
  public class EditorBuildSettingsScene { public EditorBuildSettingsScene(string p,bool e){} public bool enabled; public string path; }
  public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; }
  public static class EditorUtility { public static void SetDirty(Object o){} public static void RevealInFinder(string p){} }
  public static class SessionState { public static bool GetBool(string k,bool d)=>d; public static void SetBool(string k,bool v){} }
  public static class EditorApplication { public static bool isPlaying, isPlayingOrWillChangePlaymode; public static Action delayCall; }
  public struct BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildOptions options; }
  public enum BuildTarget { StandaloneWindows64, StandaloneOSX } public enum BuildOptions { None }
  public static class BuildPipeline { public static UnityEditor.Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions o)=>null; }
}
namespace UnityEditor.Build.Reporting { public class BuildReport { public BuildSummary summary; } public struct BuildSummary { public BuildResult result; public ulong totalSize; } public enum BuildResult { Succeeded } }
namespace UnityEditor.SceneManagement {
  public static class EditorSceneManager { public static bool SaveCurrentModifiedScenesIfUserWantsTo()=>true; public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s,NewSceneMode m)=>default(UnityEngine.SceneManagement.Scene); public static bool SaveScene(UnityEngine.SceneManagement.Scene s,string p)=>true; public static UnityEngine.SceneManagement.Scene OpenScene(string p)=>default(UnityEngine.SceneManagement.Scene); }
  public enum NewSceneSetup { EmptyScene } public enum NewSceneMode { Single }
}
namespace Steamworks {
  public struct AppId_t { public AppId_t(uint v){} } public enum ESteamAPIInitResult { k_ESteamAPIInitResult_OK }
  public static class SteamAPI { public static bool RestartAppIfNecessary(AppId_t a)=>false; public static ESteamAPIInitResult InitEx(out string e){e="";return default(ESteamAPIInitResult);} public static void RunCallbacks(){} public static void Shutdown(){} }
  public static class Packsize { public static bool Test()=>true; } public static class SteamFriends { public static string GetPersonaName()=>""; }
}
namespace UnityEngine { public class PropertyAttribute : System.Attribute {} }
namespace UnityEditor { public class CustomPropertyDrawer : System.Attribute { public CustomPropertyDrawer(System.Type t){} }
  public class SerializedProperty {} public class PropertyDrawer { public UnityEngine.PropertyAttribute attribute; public virtual void OnGUI(UnityEngine.Rect p, SerializedProperty s, UnityEngine.GUIContent l){} public virtual float GetPropertyHeight(SerializedProperty s, UnityEngine.GUIContent l)=>0; }
  public static class EditorGUI { public static bool PropertyField(UnityEngine.Rect r, SerializedProperty p, UnityEngine.GUIContent l, bool c)=>true; public static float GetPropertyHeight(SerializedProperty p, UnityEngine.GUIContent l, bool c)=>0; } }

namespace UnityEngine { public enum ScaleMode { ScaleToFit, ScaleAndCrop, StretchToFill }
  public class TextAsset : Object { public string text; }
  public static class JsonUtility { public static string ToJson(object o,bool p)=>""; public static T FromJson<T>(string j)=>default(T); }
  public static class ColorUtility { public static bool TryParseHtmlString(string s, out Color c){ c=default(Color); return true; } } }

namespace UnityEngine { public class RectOffset { public int left,right,top,bottom; public RectOffset(){} public RectOffset(int a,int b,int c,int d){left=a;right=b;top=c;bottom=d;} } }
namespace UnityEngine { public class Material : Object { public Material(Shader s){} } }
