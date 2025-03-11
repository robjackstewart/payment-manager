import { Response as CreateUserResponse, CreateUser } from "@/clients/payment-manager-api/models";
import { Response as GetUserResponse } from "@/clients/payment-manager-api/models";
import { getPaymentManagerApiClient } from "./common/factory";

export async function getUser(id: string): Promise<GetUserResponse | undefined> {
    const client = getPaymentManagerApiClient();
    const response = await client.api.users.byId(id).get()
    return response;
}

export async function createUser(request: CreateUser): Promise<CreateUserResponse | undefined> {
    const client = getPaymentManagerApiClient();
    const response = await client.api.user.post(request);
    return response;
}