export interface Config {
    paymentManagerApi: {
        baseUrl: string;
    }
}

export function getConfig(): Config {
    return {
        paymentManagerApi: {
            baseUrl: process.env.NEXT_PUBLIC_PAYMENT_MANAGER_API_BASE_URL!
        }
    }
}