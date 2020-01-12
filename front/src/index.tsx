import firebase from 'firebase/app';
import 'firebase/auth';
import 'firebase/firestore';
import React, { useContext } from 'react';
import ReactDOM from 'react-dom';
import { Redirect, useHistory } from 'react-router';
import { BrowserRouter as Router, Link, Route, RouteProps, Switch } from 'react-router-dom';
import firebaseConfig from '../credentials/firebase-client.json';

// func host start --port 7071
const apiHost = 'http://localhost:7071';

firebase.initializeApp(firebaseConfig);

const firebaseReducer = (
    state: FirebaseUserState,
    action: FirebaseUserReducerAction
): FirebaseUserState => {
    switch (action.type) {
        case 'AUTH:SET_USER':
            return { ...state, user: action.user };
        case 'AUTH:SET_PROGRESS':
            return { ...state, isProgress: action.isProgress };
        default:
            return state;
    }
};

interface FirebaseUserState {
    user: firebase.User | null;
    isProgress: boolean;
}

const initialFirebaseUserState: FirebaseUserState = {
    user: null,
    isProgress: true,
};

type FirebaseUserReducerAction =
    | {
          type: 'AUTH:SET_USER';
          user: firebase.User | null;
      }
    | {
          type: 'AUTH:SET_PROGRESS';
          isProgress: boolean;
      };

const useFirebaseAuth = () => {
    const [state, dispatch] = React.useReducer(firebaseReducer, initialFirebaseUserState);

    React.useEffect(() => {
        const disposable = firebase.auth().onIdTokenChanged(
            (maybeUser) => {
                dispatch({
                    type: 'AUTH:SET_USER',
                    user: maybeUser,
                });
                dispatch({
                    type: 'AUTH:SET_PROGRESS',
                    isProgress: false,
                });
            },
            (e) => {
                console.warn(`Firebase Auth State Changed fired error: ${e}`);
            }
        );

        return () => disposable();
    }, []);

    const signInAsAnonymously = React.useCallback(
        () =>
            firebase
                .auth()
                .signInAnonymously()
                .then((uc) => {
                    dispatch({
                        type: 'AUTH:SET_USER',
                        user: uc.user,
                    });
                    dispatch({
                        type: 'AUTH:SET_PROGRESS',
                        isProgress: false,
                    });
                    if (uc.user === null) {
                        throw Error('firebaseUser is null');
                    }
                    return uc.user;
                })
                .catch((e) => {
                    console.warn(`cannot login with anonymous: ${e}`);
                    throw e;
                }),
        []
    );

    const signInWithCustomToken = React.useCallback(
        (customToken: string) =>
            firebase
                .auth()
                .signInWithCustomToken(customToken)
                .then((uc) => {
                    dispatch({
                        type: 'AUTH:SET_USER',
                        user: uc.user,
                    });
                    dispatch({
                        type: 'AUTH:SET_PROGRESS',
                        isProgress: false,
                    });
                    if (uc.user === null) {
                        throw Error('firebaseUser is null');
                    }
                    return uc.user;
                })
                .catch((e) => {
                    console.warn(`cannot login with customToken: ${e}`);
                    throw e;
                }),
        []
    );

    // connect already signIn user..

    return { state, signInAsAnonymously, signInWithCustomToken };
};

type AuthContextValues = {
    state: FirebaseUserState;
    signInAsAnonymously: () => Promise<firebase.User>;
    signInWithCustomToken: (_: string) => Promise<firebase.User>;
};

const AuthContext = React.createContext<AuthContextValues>((null as unknown) as AuthContextValues);

const AuthProvider = (props: React.PropsWithChildren<{}>) => {
    const firebaseAuth = useFirebaseAuth();
    return <AuthContext.Provider value={firebaseAuth}>{props.children}</AuthContext.Provider>;
};

const choices = [
    { pokedexId: 1, name: 'サルノリ' },
    { pokedexId: 4, name: 'ヒバニー' },
    { pokedexId: 7, name: 'メッソン' },
];

const Inquiry = () => {
    const [inquiryResult, setInquiryResult] = React.useState<{ id: string; pokedexId: number }[]>(
        []
    );
    const auth = useContext(AuthContext);

    React.useEffect(() => {
        if (auth.state.user === null) {
            return;
        }
        const disposable = firebase
            .firestore()
            .collection('users')
            .doc(auth.state.user.uid)
            .collection('inquires')
            .onSnapshot((snapshot) => {
                const data = snapshot.docs.map((d) => ({
                    id: d.id,
                    pokedexId: d.data().pokedexId,
                }));
                setInquiryResult(data);
            });

        return () => disposable();
    }, [auth.state.user]);

    const onToggleInquiryAnswer = React.useCallback((pokedexId: number, docId?: string) => {
        if (auth.state.user === null) {
            return;
        }
        if (docId === undefined) {
            firebase
                .firestore()
                .collection('users')
                .doc(auth.state.user.uid)
                .collection('inquires')
                .add({
                    pokedexId,
                })
                .catch((e) => {
                    console.warn(`cannot add Inquiry answer: ${e}`);
                });
        } else {
            firebase
                .firestore()
                .collection('users')
                .doc(auth.state.user.uid)
                .collection('inquires')
                .doc(docId)
                .delete()
                .catch((e) => {
                    console.warn(`cannot delete Inquiry answer: ${e}`);
                });
        }
    }, []);

    if (auth.state.user === null) {
        return null;
    }

    return (
        <div>
            <p>{`ようこそ ${auth.state.user.isAnonymous ? 'ゲスト' : '会員'}さん`}</p>
            <h4>あなたの最初に選んだポケモンを教えて下さい</h4>
            <ul>
                {choices.map((c) => (
                    <li
                        onClick={() => {
                            const result = inquiryResult.find((r) => r.pokedexId === c.pokedexId);
                            if (result) {
                                onToggleInquiryAnswer(c.pokedexId, result.id);
                            } else {
                                onToggleInquiryAnswer(c.pokedexId, undefined);
                            }
                        }}
                    >
                        {`${c.name} ${
                            inquiryResult.find((r) => r.pokedexId === c.pokedexId) ? '←' : ''
                        }`}
                    </li>
                ))}
            </ul>
        </div>
    );
};

const Top = () => (
    <div>
        <h1>{'top'}</h1>
        コンテンツ
        <ul>
            <li>
                <Link to="/Inquiry">アンケート</Link>
            </li>
        </ul>
    </div>
);

const Login = () => {
    const { state, signInAsAnonymously } = React.useContext(AuthContext);
    const [isProgress, setIsProgress] = React.useState<boolean>(false);
    const [isError, setIsError] = React.useState<boolean>(false);

    const generateAndMoveLINELogin = React.useCallback(() => {
        setIsProgress(true);
        setIsError(false);
        (state.user !== null
            ? state.user.getIdToken()
            : signInAsAnonymously().then((v) => v.getIdToken())
        )
            .then((token) =>
                fetch(`${apiHost}/api/GenerateAuthorizeRequestUrl`, {
                    headers: new Headers([['Authorization', `Bearer ${token}`]]),
                    mode: 'cors',
                })
            )
            .then((res) => res.json())
            .then((json) => (window.location.href = json.url))
            .catch((e) => {
                console.warn(`cannot get lineLoginUrl: ${e}`);
                setIsProgress(false);
                setIsError(true);
            });
    }, [state, signInAsAnonymously]);

    // Todo: isErrorのハンドリング
    console.log(`isError?: ${isError}`);

    return (
        <div>
            <button disabled={isProgress} onClick={generateAndMoveLINELogin}>
                LINEでログインする
            </button>
        </div>
    );
};

const CallbackLINELogin = () => {
    const [responseReceived, setResponseReceived] = React.useState<boolean>(false);
    const [isError, setIsError] = React.useState<boolean>(false);
    const history = useHistory();
    const { state, signInWithCustomToken } = React.useContext(AuthContext);
    const firebaseTokenPromise = useFirebaseToken(state);

    React.useEffect(() => {
        const params = new URLSearchParams(window.location.search);
        if (params.has('error')) {
            setIsError(true);
            setResponseReceived(true);
            return;
        }

        firebaseTokenPromise
            .then((token) =>
                fetch(`${apiHost}/api/LineTokenVerify`, {
                    method: 'POST',
                    body: JSON.stringify({
                        code: params.get('code'),
                        state: params.get('state'),
                    }),
                    headers: new Headers([['Authorization', `Bearer ${token}`]]),
                    mode: 'cors',
                })
            )
            .then((v) => v.json())
            .then((v) => signInWithCustomToken(v.customToken))
            .then((_user) => {
                setResponseReceived(true);
                history.replace('/');
            })
            .catch((e) => {
                console.warn(e);
                setIsError(true);
                setResponseReceived(true);
            });
    }, []);

    // 認証状態を確認していますみたいなロード画面挟む
    console.log(responseReceived, isError);

    return <div>コールバックのやつ</div>;
};

const App = () => (
    <Router>
        <AuthProvider>
            <AuthProgressGuard>
                <Switch>
                    <Route exact path="/" component={Top} />
                    <AuthGuardRedirectRoute exact path="/inquiry" component={Inquiry} />
                    <Route exact path="/login" component={Login} />
                    <AuthGuardRedirectRoute path="/callback/line" component={CallbackLINELogin} />
                    <Route component={Top} />
                </Switch>
            </AuthProgressGuard>
        </AuthProvider>
    </Router>
);

const AuthProgressGuard = (props: React.PropsWithChildren<{}>) => {
    const auth = React.useContext(AuthContext);
    if (auth.state.isProgress) {
        return <div>{'Now loading...'}</div>;
    }

    return <>{props.children}</>;
};

const AuthGuardRedirectRoute = ({
    component: Component,
    redirectTo,
    ...rest
}: {
    component: React.ElementType;
    redirectTo?: string;
} & RouteProps) => {
    const auth = React.useContext(AuthContext);

    return (
        <Route
            {...rest}
            render={(props) =>
                auth.state.user !== null ? (
                    <Component {...props} />
                ) : redirectTo === undefined ? (
                    <Redirect to="/" />
                ) : (
                    <Redirect to={redirectTo} />
                )
            }
        />
    );
};

const useFirebaseToken = (state: FirebaseUserState) => {
    const [needRefresh, setNeedRefresh] = React.useState<boolean>(true);
    const [token, setToken] = React.useState<string>('');
    React.useEffect(() => setNeedRefresh(true), [state]);

    if (state.user === null) {
        return Promise.reject('login needed');
    }

    const tokenPromise =
        needRefresh || token === ''
            ? state.user.getIdToken().then((v) => {
                  setToken(v);
                  setNeedRefresh(false);
                  return v;
              })
            : Promise.resolve(token);

    return tokenPromise;
};

ReactDOM.render(<App />, document.querySelector('#root'));
