'use server';
import { getUser } from "@/app/actions";

export default async function Page({
    params,
}: {
    params: Promise<{ userId: string }>
}) {
    const { userId } = await params;
    const result = await getUser(userId);
    return (
        <div>Name: {result?.name}</div>
    );
}
