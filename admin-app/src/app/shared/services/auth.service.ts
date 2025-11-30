import { Injectable } from "@angular/core";
import { BaseService } from "./base.service";
import { BehaviorSubject } from "rxjs";
import { User, UserManager } from "oidc-client";


@Injectable({
    providedIn: "root"
})
export class AuthService extends BaseService {

    private _authNavStatusSource = new BehaviorSubject<boolean>(false);
    authNavStatus$ = this._authNavStatusSource.asObservable();

    private manager = new UserManager(getClientSettings());
    private user: User | null;

    constructor() {
        super();
        this.manager.getUser().then(user => {
            this.user = user;
            this._authNavStatusSource.next(this.isAuthenticated());
        });
    }

    login() {
        return this.manager.signinRedirect();
    }

    async completeAuthentication() {
        this.user = await this.manager.signinRedirectCallback();
        this._authNavStatusSource.next(this.isAuthenticated());
    }

    isAuthenticated(): boolean {
        return this.user != null && !this.user.expired;
    }

    get authorizationHeaderValue(): string | null {
        if (this.user ) {
            return `${this.user.token_type} ${this.user.access_token}`;
        }
        return null;
    }

    get name(): string | null {
        return this.user ? this.user.profile.name : null;
    }

    get profile(): any {
        if (this.user != null && this.user.profile) {
            return {
                ...this.user.profile,
                userName: this.user.profile.name || this.user.profile.preferred_username || '',
                role: this.user.profile.role || 'user',
                permissions: this.user.profile.permissions || []
            };
        }
        return null;
    }

    async signout(){
        await this.manager.signoutRedirect();
    }
}

export function getClientSettings() {
    return {
        authority: "https://localhost:5000",
        client_id: "angular_admin",
        redirect_uri: "http://localhost:4200/auth-callback",
        post_logout_redirect_uri: "http://localhost:4200/",
        response_type: "code",
        scope:"openid profile api.knowledgespace",
        filterProtocolClaims: true,
        loadUserInfo: true,
        automaticSilentRenew: true,
        silent_redirect_uri: "http://localhost:4200/silent-renew.html"
    };
}