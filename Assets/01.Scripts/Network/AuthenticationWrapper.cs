using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

public enum AuthState
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    Error,
    TimeOut
}

public static class AuthenticationWrapper
{
    public static AuthState State { get; private set; } = AuthState.NotAuthenticated;

    public static event Action<string> OnMessageEvent;

    public static async Task<AuthState> DoAuth(int maxTries = 5)
    {
        if (State == AuthState.Authenticated)  //인증된 유저라면
        {
            return State;
        }

        if (State == AuthState.Authenticating) // 현재 인증중이야.
        {
            OnMessageEvent?.Invoke("인증이 진행중입니다.");
            return await Authenticating();
        }

        //여기엔 실제 인증로직이 들어가야겠지
        await SignInAnonymouslyAsync(maxTries);
        return State;
    }
    
    private static async Task SignInAnonymouslyAsync(int maxTries)
    {
        State = AuthState.Authenticating; //인증시작으로 변경하고

        int tries = 0; 
        while(State == AuthState.Authenticating && tries < maxTries)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                //인증이 성공했을 때는 더이상 루프돌지 말고 종료
                if(AuthenticationService.Instance.IsSignedIn
                    && AuthenticationService.Instance.IsAuthorized)
                {
                    State = AuthState.Authenticated;
                    break;
                }
            }catch(AuthenticationException ex) //유니티 서버 인증에러
            {
                OnMessageEvent?.Invoke(ex.Message);
                State = AuthState.Error;
                break;
            }catch(RequestFailedException ex) //인터넷 연결 에러
            {
                OnMessageEvent?.Invoke(ex.Message);
                State = AuthState.Error;
                break;
            }

            ++tries;
            await Task.Delay(1000); //1초에 한번씩 인증을 시도
        }

        if(State != AuthState.Authenticated && tries == maxTries)
        {
            OnMessageEvent?.Invoke($"Auth timeout : {tries} tries");
            State = AuthState.TimeOut;
        }
    }

    private static async Task<AuthState> Authenticating()
    {
        while(State == AuthState.Authenticating)
        {
            await Task.Delay(200); //0.2초마다 한번씩 확인
        }
        return State;
    }
}
