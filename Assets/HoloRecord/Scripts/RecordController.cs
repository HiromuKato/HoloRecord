using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace HoloRecord
{
    /// <summary>
    /// HoloLensの録画を制御する
    /// </summary>
    public class RecordController : MonoBehaviour
    {
        /// <summary>
        /// デバイスポータルのURL(Wi-Fi接続時)
        /// </summary>
        [SerializeField]
        private string url = "192.168.0."; // ← ★ここを入力する（または入力UIを作成する）

        /// <summary>
        /// デバイスポータルのユーザ名
        /// </summary>
        [SerializeField]
        private string usr = ""; // ← ★ここを入力する（または入力UIを作成する）

        /// <summary>
        /// デバイスポータルのパスワード
        /// </summary>
        [SerializeField]
        private string pass = ""; // ← ★ここを入力する（または入力UIを作成する）

        /// <summary>
        /// Wi-Fi接続時に必要なトークン
        /// </summary>
        private string csrfToken = "";

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
        /// 録画中かどうか
        /// </summary>
        private bool isRecording = false;

        /// <summary>
        /// デバッグ用テキスト表示
        /// </summary>
        public TextMesh textMesh;

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Start()
        {
            StartCoroutine(AuthCoroutine());
        }

        /// <summary>
        /// 録画を開始する
        /// </summary>
        public void StartRecord()
        {
            if (isRecording)
            {
                return;
            }
            StartCoroutine(StartRecordCoroutine());
        }

        /// <summary>
        /// 録画を停止する
        /// </summary>
        public void StopRecord()
        {
            isRecording = false;
            StartCoroutine(StopRecordCoroutine());
        }

        /// <summary>
        /// 認証に必要なCSRF-Tokenを取得するリクエスト処理を行う
        /// </summary>
        private IEnumerator AuthCoroutine()
        {
            UnityWebRequest request = UnityWebRequest.Get("https://" + url);
            request.SetRequestHeader("Authorization", MakeAuthorizationString(usr, pass));
            yield return request.SendWebRequest();

            if (request.isNetworkError)
            {
                textMesh.text = request.error;
            }
            else
            {
                textMesh.text = "認証レスポンス受信";
                csrfToken = request.GetResponseHeader("Set-Cookie");
            }
        }

        /// <summary>
        /// 録画開始のPOSTリクエスト処理を行う
        /// </summary>
        private IEnumerator StartRecordCoroutine()
        {
            WWWForm form = new WWWForm();
            string api = "/api/holographic/mrc/video/control/start";
            string parameter =
                "?holo=" + holo.ToString().ToLower() +
                "&pv=" + pv.ToString().ToLower() +
                "&mic=" + mic.ToString().ToLower() +
                "&loopback=" + loopback.ToString().ToLower();

            UnityWebRequest request = UnityWebRequest.Post("http://" + url + api + parameter, form);
            request.SetRequestHeader("Authorization", MakeAuthorizationString(usr, pass));
            request.SetRequestHeader("X-CSRF-Token", csrfToken.Replace("CSRF-Token=", ""));
            yield return request.SendWebRequest();

            if (request.isNetworkError)
            {
                textMesh.text = request.error;
            }
            else
            {
                textMesh.text = "録画開始レスポンス受信";
                isRecording = true;
            }
        }

        /// <summary>
        /// 録画停止のPOSTリクエスト処理を行う
        /// </summary>
        private IEnumerator StopRecordCoroutine()
        {
            WWWForm form = new WWWForm();
            string api = "/api/holographic/mrc/video/control/stop";
            UnityWebRequest request = UnityWebRequest.Post("http://" + url + api, form);
            request.SetRequestHeader("Authorization", MakeAuthorizationString(usr, pass));
            request.SetRequestHeader("x-csrf-token", csrfToken.Replace("CSRF-Token=", ""));
            yield return request.SendWebRequest();

            if (request.isNetworkError)
            {
                textMesh.text = request.error;
            }
            else
            {
                textMesh.text = "録画終了レスポンス受信";
            }
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

    } // class
} // namespace

