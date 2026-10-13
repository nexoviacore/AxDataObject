using FirebaseAdmin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


using System.IO;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

using System.Net.Http;
using ARM_APIs.Interface;


public class FirebaseTokenService : IFirebase
{
    private static GoogleCredential _credential;
    public async Task<string> GetAccessTokenAsync()
    {
        if (_credential == null)
        {
            _credential = GoogleCredential
                .FromFile("service_key.json")
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
        }

        return await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
    }
}