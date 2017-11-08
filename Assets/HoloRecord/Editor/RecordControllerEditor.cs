using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

namespace HoloRecord
{
    /// <summary>
    /// HoloLensの録画を制御するエディタ拡張
    /// </summary>
    public class RecordControllerEditor : EditorWindow
    {
        /// <summary>
        /// デバイスポータルのURL(Wi-Fi接続時)
        /// </summary>
        private string url = "192.168.0.";
        // デバイスポータルのURL(USB接続時)
        //private string url = "127.0.0.1:10080";

        /// <summary>
        /// Wi-Fi接続時に必要なトークン
        /// </summary>
        private string csrfToken = "";

        /// <summary>
        /// CsrfTokenを取得したかどうか
        /// </summary>
        private bool gotToken = false;

        /// <summary>
        /// デバイスポータルのユーザ名
        /// </summary>
        private string usr = "";

        /// <summary>
        /// デバイスポータルのパスワード
        /// </summary>
        private string pass = "";

        /// <summary>
        /// ホログラムをキャプチャするかどうか
        /// </summary>
        private bool holo = true;

        /// <summary>
        /// カメラ画像をキャプチャするかどうか
        /// </summary>
        private bool pv = true;

        /// <summary>
        /// マイクを利用するかどうか
        /// </summary>
        private bool mic = true;

        /// <summary>
        /// アプリケーションのオーディオを録音するかどうか
        /// </summary>
        private bool loopback = true;

        /// <summary>
        /// ボタンラベル
        /// </summary>
        private string buttonLabel = "Record";

        /// <summary>
        /// 録画中かどうか
        /// </summary>
        private bool isRecording = false;

        /// <summary>
        /// 録画中は設定項目を編集不可にする
        /// </summary>
        private bool disabled = false;

        /// <summary>
        /// 録画時間
        /// </summary>
        private float recordTime = 5.0f;

        /// <summary>
        /// 録画終了時間
        /// </summary>
        private float endTime;

        /// <summary>
        /// 認証処理リクエスト
        /// </summary>
        private UnityWebRequest authReq;

        /// <summary>
        /// 録画開始リクエスト
        /// </summary>
        private UnityWebRequest startReq;

        /// <summary>
        /// 録画停止リクエスト
        /// </summary>
        private UnityWebRequest stopReq;

        /// <summary>
        /// Windowを表示する
        /// </summary>
        [MenuItem("Tool/RecordContoller")]
        public static void ShowWindow()
        {
            // すでにWindowが存在すればそのインスタンスを取得し、なければ生成する
            var window = EditorWindow.GetWindow(typeof(RecordControllerEditor));

            // Windowのサイズを変更不可にする
            window.maxSize = window.minSize = new Vector2(400, 260);

            // Windowのタイトルを設定する
            window.titleContent = new GUIContent("Recorder");
        }

        /// <summary>
        /// オブジェクトがロードされたときに呼ばれる
        /// </summary>
        private void OnEnable()
        {
            if (gotToken == false)
            {
                // パスワードを入力し終えたら認証としたほうが好ましいが、
                // デバイスポータル情報をハードコードした場合こちらのほうが楽
                SendAuthRequest();
            }
        }

        /// <summary>
        /// キーボードフォーカスされたときに呼ばれる
        /// </summary>
        private void OnFocus()
        {
            if (gotToken == false)
            {
                SendAuthRequest();
            }
        }

        /// <summary>
        /// GUIを描画する
        /// </summary>
        private void OnGUI()
        {
            // 編集制御グループ
            EditorGUI.BeginDisabledGroup(disabled);
            {
                GUILayout.Label("DevicePortal Information", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                {
                    url = EditorGUILayout.TextField("Url", url);
                    usr = EditorGUILayout.TextField("User Name", usr);
                    pass = EditorGUILayout.PasswordField("Password", pass);
                }
                EditorGUI.indentLevel--;
                GUILayout.Space(10);

                GUILayout.Label("Record Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                {
                    holo = EditorGUILayout.ToggleLeft("ホログラムをキャプチャする", holo);
                    pv = EditorGUILayout.ToggleLeft("カメラ画像をキャプチャする", pv);
                    mic = EditorGUILayout.ToggleLeft("マイクを利用する", mic);
                    loopback = EditorGUILayout.ToggleLeft("アプリのオーディオを録音する", loopback);
                    recordTime = EditorGUILayout.Slider("録画時間(秒)", recordTime, 1, 300);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(20);

            // ボタン処理
            if (GUILayout.Button(buttonLabel))
            {
                if (isRecording)
                {
                    StopRecord();
                }
                else
                {
                    StartRecord();
                }
            }
        }

        /// <summary>
        /// 録画を開始する
        /// </summary>
        private void StartRecord()
        {
            buttonLabel = "Stop";
            disabled = true;

            SendStartRecordRequest();
        }

        /// <summary>
        /// 録画を停止する
        /// </summary>
        private void StopRecord()
        {
            buttonLabel = "Record";
            disabled = false;
            isRecording = false;
            endTime = 0.0f;
            // プログレスバーを削除する
            EditorUtility.ClearProgressBar();

            SendStopRecordRequest();
        }

        /// <summary>
        /// 認証に必要なCSRF-Tokenを取得するリクエスト送信を行う
        /// </summary>
        private void SendAuthRequest()
        {
            authReq = UnityWebRequest.Get("https://" + url);
            authReq.SetRequestHeader("Authorization", MakeAuthorizationString(usr, pass));
            authReq.SendWebRequest();
        }

        /// <summary>
        /// 録画開始のPOSTリクエスト送信を行う
        /// </summary>
        private void SendStartRecordRequest()
        {
            WWWForm form = new WWWForm();
            string api = "/api/holographic/mrc/video/control/start";
            string parameter =
                "?holo=" + holo.ToString().ToLower() +
                "&pv=" + pv.ToString().ToLower() +
                "&mic=" + mic.ToString().ToLower() +
                "&loopback=" + loopback.ToString().ToLower();

            startReq = UnityWebRequest.Post("http://" + url + api + parameter, form);
            startReq.SetRequestHeader("Authorization", MakeAuthorizationString(usr, pass));
            startReq.SetRequestHeader("X-CSRF-Token", csrfToken.Replace("CSRF-Token=", ""));
            startReq.SendWebRequest();
        }

        /// <summary>
        /// 録画停止のPOSTリクエスト送信を行う
        /// </summary>
        private void SendStopRecordRequest()
        {
            WWWForm form = new WWWForm();
            string api = "/api/holographic/mrc/video/control/stop";
            stopReq = UnityWebRequest.Post("http://" + url + api, form);
            stopReq.SetRequestHeader("Authorization", MakeAuthorizationString(usr, pass));
            stopReq.SetRequestHeader("x-csrf-token", csrfToken.Replace("CSRF-Token=", ""));
            stopReq.SendWebRequest();
        }

        /// <summary>
        /// ユーザ情報からベーシック認証の認証文字列を生成する
        /// </summary>
        /// <param name="username">ユーザ名</param>
        /// <param name="password">パスワード</param>
        /// <returns>認証文字列</returns>
        private string MakeAuthorizationString(string username, string password)
        {
            string auth = username + ":" + password;
            auth = System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(auth));
            auth = "Basic " + auth;
            return auth;
        }

        /// <summary>
        /// 毎フレーム処理を行う
        /// </summary>
        private void Update()
        {
            // エラー処理
            if (IsError()) return;

            // レスポンス受信後の処理
            HandleResponse();

            // Unityエディタが起動されてからの経過時間
            float timeSinceStartup = (float)EditorApplication.timeSinceStartup;

            if (isRecording)
            {
                // プログレスバー表示
                float leftTime = endTime - timeSinceStartup;
                float progress = (recordTime - leftTime) / recordTime;
                EditorUtility.DisplayProgressBar("録画中", "残り時間 " + leftTime.ToString("0.0") + "秒", progress);

                if (endTime <= timeSinceStartup)
                {
                    // 設定した時間が経過したので録画停止する
                    StopRecord();
                }
            }
        }

        /// <summary>
        /// エラーチェックを行う
        /// </summary>
        /// <returns>エラーの場合trueを返す</returns>
        private bool IsError()
        {
            if (IsRequestError(authReq)) return true;
            if (IsRequestError(startReq)) return true;
            if (IsRequestError(stopReq)) return true;
            return false;
        }

        /// <summary>
        /// リクエストのエラーチェックを行う
        /// </summary>
        /// <param name="request">リクエスト</param>
        /// <returns>エラーの場合trueを返す</returns>
        private bool IsRequestError(UnityWebRequest request)
        {
            if (request != null && request.error != null)
            {
                // 表示され続けるので一応コメント
                //Debug.Log("There was an error sending request to " + request.url + " : " + request.error + " " + request.isNetworkError);
                return true;
            }
            return false;
        }

        /// <summary>
        /// レスポンス処理を行う
        /// </summary>
        private void HandleResponse()
        {
            if (authReq != null && authReq.isDone)
            {
                Debug.Log("認証レスポンス受信");
                /*
                Dictionary<string, string> dic = authReq.GetResponseHeaders();
                foreach (KeyValuePair<string, string> pair in dic)
                {
                    Debug.Log(pair.Key + " : " + pair.Value);
                }
                */
                csrfToken = authReq.GetResponseHeader("Set-Cookie");
                gotToken = true;
                authReq = null;
            }

            if (startReq != null && startReq.isDone)
            {
                Debug.Log("録画開始レスポンス受信");
                // 終了時間を設定
                endTime = (float)EditorApplication.timeSinceStartup + recordTime;
                isRecording = true;
                startReq = null;
            }

            if (stopReq != null && stopReq.isDone)
            {
                Debug.Log("録画終了レスポンス受信");
                stopReq = null;
            }
        }

    } // class
} // namespace