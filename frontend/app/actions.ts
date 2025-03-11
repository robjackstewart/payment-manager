import { GetUserResponse, CreateUserResponse, CreateUser } from "@/clients/payment-manager-api/models";
import { getPaymentManagerApiClient } from "./factory";

const paymentManagerApiClient = getPaymentManagerApiClient();

export function getUser(id: string): Promise<GetUserResponse | undefined> {
    return paymentManagerApiClient.api.users.byId(id).get();
}

export function createUser(request: CreateUser): Promise<CreateUserResponse | undefined> {
    return paymentManagerApiClient.api.user.post(request);
}