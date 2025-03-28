/* tslint:disable */
/* eslint-disable */
// Generated by Microsoft Kiota
// @ts-ignore
import { createCreateUserResponseFromDiscriminatorValue, createProblemDetailsFromDiscriminatorValue, serializeCreateUser, serializeCreateUserResponse, type CreateUser, type CreateUserResponse, type ProblemDetails } from '../../models/index.js';
// @ts-ignore
import { type BaseRequestBuilder, type Parsable, type ParsableFactory, type RequestConfiguration, type RequestInformation, type RequestsMetadata } from '@microsoft/kiota-abstractions';

/**
 * Builds and executes requests for operations under /api/user
 */
export interface UserRequestBuilder extends BaseRequestBuilder<UserRequestBuilder> {
    /**
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @returns {Promise<CreateUserResponse>}
     * @throws {ProblemDetails} error when the service returns a 400 status code
     */
     post(body: CreateUser, requestConfiguration?: RequestConfiguration<object> | undefined) : Promise<CreateUserResponse | undefined>;
    /**
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @returns {RequestInformation}
     */
     toPostRequestInformation(body: CreateUser, requestConfiguration?: RequestConfiguration<object> | undefined) : RequestInformation;
}
/**
 * Uri template for the request builder.
 */
export const UserRequestBuilderUriTemplate = "{+baseurl}/api/user";
/**
 * Metadata for all the requests in the request builder.
 */
export const UserRequestBuilderRequestsMetadata: RequestsMetadata = {
    post: {
        uriTemplate: UserRequestBuilderUriTemplate,
        responseBodyContentType: "application/json",
        errorMappings: {
            400: createProblemDetailsFromDiscriminatorValue as ParsableFactory<Parsable>,
        },
        adapterMethodName: "send",
        responseBodyFactory:  createCreateUserResponseFromDiscriminatorValue,
        requestBodyContentType: "application/json",
        requestBodySerializer: serializeCreateUser,
        requestInformationContentSetMethod: "setContentFromParsable",
    },
};
/* tslint:enable */
/* eslint-enable */
