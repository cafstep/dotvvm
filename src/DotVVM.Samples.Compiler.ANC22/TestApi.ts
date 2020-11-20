﻿namespace Test {
    class ClientBase {
        public transformOptions(options: RequestInit) {
            options.credentials = "same-origin";
            return Promise.resolve(options);
        }
    }
    /* tslint:disable */
    //----------------------
    // <auto-generated>
    //     Generated using the NSwag toolchain v11.14.1.0 (NJsonSchema v9.10.24.0 (Newtonsoft.Json v12.0.0.0)) (http://NSwag.org)
    // </auto-generated>
    //----------------------
    // ReSharper disable InconsistentNaming
    
    export class TestApi extends ClientBase {
        private http: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> };
        private baseUrl: string;
        protected jsonParseReviver: (key: string, value: any) => any = undefined;
    
        constructor(baseUrl?: string, http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> }) {
            super();
            this.http = http ? http : <any>window;
            this.baseUrl = baseUrl ? baseUrl : "";
        }
    
        /**
         * @return Success
         */
        get(): Promise<string[]> {
            let url_ = this.baseUrl + "/api/Values";
            url_ = url_.replace(/[?&]$/, "");
    
            let options_ = <RequestInit>{
                method: "GET",
                headers: new Headers({
                    "Content-Type": "application/json", 
                    "Accept": "application/json"
                })
            };
    
            return this.transformOptions(options_).then(transformedOptions_ => {
                return this.http.fetch(url_, transformedOptions_);
            }).then((_response: Response) => {
                return this.processGet(_response);
            });
        }
    
        protected processGet(response: Response): Promise<string[]> {
            const status = response.status;
            let _headers: any = {}; if (response.headers && response.headers.forEach) { response.headers.forEach((v, k) => _headers[k] = v); };
            if (status === 200) {
                return response.text().then((_responseText) => {
                let result200: any = null;
                let resultData200 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
                if (resultData200 && resultData200.constructor === Array) {
                    result200 = [];
                    for (let item of resultData200)
                        result200.push(item);
                }
                return result200;
                });
            } else if (status !== 200 && status !== 204) {
                return response.text().then((_responseText) => {
                return throwException("An unexpected server error occurred.", status, _responseText, _headers);
                });
            }
            return Promise.resolve<string[]>(<any>null);
        }
    
        /**
         * @value (optional) 
         * @return Success
         */
        post(value?: string): Promise<void> {
            let url_ = this.baseUrl + "/api/Values";
            url_ = url_.replace(/[?&]$/, "");
    
            const content_ = JSON.stringify(value);
    
            let options_ = <RequestInit>{
                body: content_,
                method: "POST",
                headers: new Headers({
                    "Content-Type": "application/json", 
                })
            };
    
            return this.transformOptions(options_).then(transformedOptions_ => {
                return this.http.fetch(url_, transformedOptions_);
            }).then((_response: Response) => {
                return this.processPost(_response);
            });
        }
    
        protected processPost(response: Response): Promise<void> {
            const status = response.status;
            let _headers: any = {}; if (response.headers && response.headers.forEach) { response.headers.forEach((v, k) => _headers[k] = v); };
            if (status === 200) {
                return response.text().then((_responseText) => {
                return;
                });
            } else if (status !== 200 && status !== 204) {
                return response.text().then((_responseText) => {
                return throwException("An unexpected server error occurred.", status, _responseText, _headers);
                });
            }
            return Promise.resolve<void>(<any>null);
        }
    }
    
    
    export class SwaggerException extends Error {
        message: string;
        status: number; 
        response: string; 
        headers: { [key: string]: any; };
        result: any; 
    
        constructor(message: string, status: number, response: string, headers: { [key: string]: any; }, result: any) {
            super();
    
            this.message = message;
            this.status = status;
            this.response = response;
            this.headers = headers;
            this.result = result;
        }
    
        protected isSwaggerException = true;
    
        static isSwaggerException(obj: any): obj is SwaggerException {
            return obj.isSwaggerException === true;
        }
    }
    
    function throwException(message: string, status: number, response: string, headers: { [key: string]: any; }, result?: any): any {
        if(result !== null && result !== undefined)
            throw result;
        else
            throw new SwaggerException(message, status, response, headers, null);
    }
}
