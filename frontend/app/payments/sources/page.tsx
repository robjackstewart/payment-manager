'use server';
import { getAllPaymentSources } from "@/app/actions";
import Link from "next/link";

export default async function Users() {
    const result = await getAllPaymentSources().catch((error) => {
        console.log(error);
    });
    return (
        <div className="relative overflow-x-auto">
            <h1>Users</h1>
            <table className="w-full text-sm text-left rtl:text-right text-gray-500 dark:text-gray-400">
                <thead className="text-xs text-gray-700 uppercase bg-gray-50 dark:bg-gray-700 dark:text-gray-400">
                    <tr className="bg-white border-b dark:bg-gray-800 dark:border-gray-700 border-gray-200">
                        <th scope="col">Id</th>
                        <th scope="col">Name</th>
                        <th scope="col">Description</th>
                        <th scope="col">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {result?.paymentSources?.map((paymentSource) => (
                        <tr key={paymentSource.id} className="bg-white border-b dark:bg-gray-800 dark:border-gray-700 border-gray-200">
                            <td>{paymentSource.id}</td>
                            <td>{paymentSource.name}</td>
                            <td>{paymentSource.description}</td>
                            <td><Link href={`/payments/sources/${paymentSource.id}`}>View</Link></td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
