'use server';
import { getPaymentSource } from "@/app/actions";

export default async function Page({
    params,
}: {
    params: Promise<{ paymentSourceId: string }>
}) {
    const { paymentSourceId } = await params;
    const result = await getPaymentSource(paymentSourceId);
    return (
        <div>
            <div>Id: {result?.id}</div>
            <div>Name: {result?.name}</div>
            <div>Description: {result?.description}</div>
        </div>
    );
}
