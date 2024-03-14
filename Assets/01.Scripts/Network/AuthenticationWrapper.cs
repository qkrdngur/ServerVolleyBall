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
        if (State == AuthState.Authenticated)  //������ �������
        {
            return State;
        }

        if (State == AuthState.Authenticating) // ���� �������̾�.
        {
            OnMessageEvent?.Invoke("������ �������Դϴ�.");
            return await Authenticating();
        }

        //���⿣ ���� ���������� ���߰���
        await SignInAnonymouslyAsync(maxTries);
        return State;
    }
    
    private static async Task SignInAnonymouslyAsync(int maxTries)
    {
        State = AuthState.Authenticating; //������������ �����ϰ�

        int tries = 0; 
        while(State == AuthState.Authenticating && tries < maxTries)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                //������ �������� ���� ���̻� �������� ���� ����
                if(AuthenticationService.Instance.IsSignedIn
                    && AuthenticationService.Instance.IsAuthorized)
                {
                    State = AuthState.Authenticated;
                    break;
                }
            }catch(AuthenticationException ex) //����Ƽ ���� ��������
            {
                OnMessageEvent?.Invoke(ex.Message);
                State = AuthState.Error;
                break;
            }catch(RequestFailedException ex) //���ͳ� ���� ����
            {
                OnMessageEvent?.Invoke(ex.Message);
                State = AuthState.Error;
                break;
            }

            ++tries;
            await Task.Delay(1000); //1�ʿ� �ѹ��� ������ �õ�
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
            await Task.Delay(200); //0.2�ʸ��� �ѹ��� Ȯ��
        }
        return State;
    }
}
