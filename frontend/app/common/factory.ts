
import { AnonymousAuthenticationProvider } from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter } from "@microsoft/kiota-http-fetchlibrary";
import { createPaymentManagerApiClient } from "@/clients/payment-manager-api/paymentManagerApiClient";
import { getConfig } from "../../config";

export function getPaymentManagerApiClient() {
    const authProvider = new AnonymousAuthenticationProvider();
    const adapter = new FetchRequestAdapter(authProvider);
    const config = getConfig();
    console.log(config);
    adapter.baseUrl = config.paymentManagerApi.baseUrl;
    console.log(adapter);
    return createPaymentManagerApiClient(adapter);
}