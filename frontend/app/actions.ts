import { GetUserResponse, CreateUserResponse, CreateUserEndpointRequest, GetAllUsersResponse, GetAllPaymentSourcesResponse, CreatePaymentSourceEndpointRequest, CreatePaymentSourceResponse, GetPaymentSourceResponse } from "@/clients/payment-manager-api/models";
import { getPaymentManagerApiClient } from "./factory";

const paymentManagerApiClient = getPaymentManagerApiClient();

export function getUser(id: string): Promise<GetUserResponse | undefined> {
    return paymentManagerApiClient.api.users.byId(id).get();
}

export function createUser(request: CreateUserEndpointRequest): Promise<CreateUserResponse | undefined> {
    return paymentManagerApiClient.api.user.post(request);
}

export function getAllUsers(): Promise<GetAllUsersResponse | undefined> {
    return paymentManagerApiClient.api.users.get();
}

export function getAllPaymentSources(): Promise<GetAllPaymentSourcesResponse | undefined> {
    return paymentManagerApiClient.api.payments.sources.get();
}

export function getPaymentSource(id: string): Promise<GetPaymentSourceResponse | undefined> {
    return paymentManagerApiClient.api.payments.sources.byId(id).get();
}

export function createPaymentSource(request: CreatePaymentSourceEndpointRequest): Promise<CreatePaymentSourceResponse | undefined> {
    return paymentManagerApiClient.api.payments.source.post(request);
}