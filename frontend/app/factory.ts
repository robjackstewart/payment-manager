
import { AnonymousAuthenticationProvider, ParseNodeFactoryRegistry } from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter } from "@microsoft/kiota-http-fetchlibrary";
import { createPaymentManagerApiClient } from "@/clients/payment-manager-api/paymentManagerApiClient";
import { getConfig } from "../config";

export function getPaymentManagerApiClient() {
    const authProvider = new AnonymousAuthenticationProvider();
    console.log(ParseNodeFactoryRegistry.defaultInstance);
    const adapter = new FetchRequestAdapter(authProvider);
    console.log(adapter);
    const config = getConfig();
    adapter.baseUrl = config.paymentManagerApi.baseUrl;
    const client = createPaymentManagerApiClient(adapter);
    console.log(client);
    return client;
}