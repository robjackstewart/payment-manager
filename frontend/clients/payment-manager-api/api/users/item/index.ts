/* tslint:disable */
/* eslint-disable */
// Generated by Microsoft Kiota
// @ts-ignore
import { createGetUserResponseFromDiscriminatorValue, createProblemDetailsFromDiscriminatorValue, type GetUserResponse, type ProblemDetails } from '../../../models/index.js';
// @ts-ignore
import { type BaseRequestBuilder, type Parsable, type ParsableFactory, type RequestConfiguration, type RequestInformation, type RequestsMetadata } from '@microsoft/kiota-abstractions';

/**
 * Builds and executes requests for operations under /api/users/{id}
 */
export interface UsersItemRequestBuilder extends BaseRequestBuilder<UsersItemRequestBuilder> {
    /**
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @returns {Promise<GetUserResponse>}
     * @throws {ProblemDetails} error when the service returns a 400 status code
     */
     get(requestConfiguration?: RequestConfiguration<object> | undefined) : Promise<GetUserResponse | undefined>;
    /**
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @returns {RequestInformation}
     */
     toGetRequestInformation(requestConfiguration?: RequestConfiguration<object> | undefined) : RequestInformation;
}
/**
 * Uri template for the request builder.
 */
export const UsersItemRequestBuilderUriTemplate = "{+baseurl}/api/users/{id}";
/**
 * Metadata for all the requests in the request builder.
 */
export const UsersItemRequestBuilderRequestsMetadata: RequestsMetadata = {
    get: {
        uriTemplate: UsersItemRequestBuilderUriTemplate,
        responseBodyContentType: "application/json",
        errorMappings: {
            400: createProblemDetailsFromDiscriminatorValue as ParsableFactory<Parsable>,
        },
        adapterMethodName: "send",
        responseBodyFactory:  createGetUserResponseFromDiscriminatorValue,
    },
};
/* tslint:enable */
/* eslint-enable */
