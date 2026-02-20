using UnityEngine;
using Mirror;
using MySql.Data.MySqlClient;
using System;
using System.Collections;

public struct AuthRequestMessage : NetworkMessage
{
    public string authUsername;
    public string authPassword;
}

public struct AuthResponseMessage : NetworkMessage
{
    public byte code;
    public string message;
}

public class MySqlAuthenticator : NetworkAuthenticator
{
    [Header("Client Credentials")]
    public string clientUsername;
    public string clientPassword;

    [Header("MySQL Settings")]
    [Tooltip("Enter the local IP (e.g., 127.0.0.1) and DB credentials.")]
    [SerializeField] private string dbServer = "127.0.0.1";
    [SerializeField] private string dbName = "programming";
    [SerializeField] private string dbUser = "root";
    [SerializeField] private string dbPassword = "";

    public string connectionString => $"Server={dbServer};Database={dbName};User={dbUser};Password={dbPassword};";

    public event Action<bool, string> OnAuthResponse;

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<AuthRequestMessage>(OnServerAuthRequestMessage, false);
    }

    public override void OnStopServer()
    {
        NetworkServer.UnregisterHandler<AuthRequestMessage>();
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnClientAuthResponseMessage, false);
    }

    public override void OnStopClient()
    {
        NetworkClient.UnregisterHandler<AuthResponseMessage>();
    }

    public override void OnServerAuthenticate(NetworkConnectionToClient conn)
    {
        // Require clients to send an AuthRequestMessage.
        // There is no automatic approval here; we wait for the message handler to process it.
    }

    private void OnServerAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
    {
        // Don't authenticate if already authenticated or disconnected
        if (conn.isAuthenticated) return;

        if (AuthenticateWithMySql(msg.authUsername, msg.authPassword))
        {
            AuthResponseMessage authResponseMessage = new AuthResponseMessage
            {
                code = 1,
                message = "Success"
            };

            conn.Send(authResponseMessage);
            ServerAccept(conn);
        }
        else
        {
            AuthResponseMessage authResponseMessage = new AuthResponseMessage
            {
                code = 0,
                message = "Invalid username or password"
            };

            conn.Send(authResponseMessage);
            conn.isAuthenticated = false;
            StartCoroutine(DelayedDisconnect(conn, 1f));
        }
    }

    private IEnumerator DelayedDisconnect(NetworkConnectionToClient conn, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        ServerReject(conn);
    }

    private bool AuthenticateWithMySql(string username, string password)
    {
        bool isValidUser = false;
        try
        {
            using (MySqlConnection dbConnection = new MySqlConnection(connectionString))
            {
                dbConnection.Open();

                string query = "SELECT * FROM user_info WHERE User_Name=@id AND User_Password=@pw";
                using (MySqlCommand cmd = new MySqlCommand(query, dbConnection))
                {
                    cmd.Parameters.AddWithValue("@id", username);
                    cmd.Parameters.AddWithValue("@pw", password);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            isValidUser = true;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MySqlAuthenticator] Database error: {e.Message}");
        }

        return isValidUser;
    }

    public override void OnClientAuthenticate()
    {
        AuthRequestMessage authRequestMessage = new AuthRequestMessage
        {
            authUsername = clientUsername,
            authPassword = clientPassword
        };

        NetworkClient.Send(authRequestMessage);
    }

    private void OnClientAuthResponseMessage(AuthResponseMessage msg)
    {
        if (msg.code == 1)
        {
            Debug.Log($"[MySqlAuthenticator] Authentication Success: {msg.message}");
            OnAuthResponse?.Invoke(true, msg.message);
            ClientAccept();
        }
        else
        {
            Debug.LogError($"[MySqlAuthenticator] Authentication Failed: {msg.message}");
            OnAuthResponse?.Invoke(false, msg.message);
            ClientReject();
        }
    }
}
