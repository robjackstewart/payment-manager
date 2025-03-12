'use server';
import { getAllUsers } from "@/app/actions";
import Link from "next/link";

export default async function Users() {
    const result = await getAllUsers().catch((error) => {
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
                        <th scope="col">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {result?.users?.map((user) => (
                        <tr key={user.id} className="bg-white border-b dark:bg-gray-800 dark:border-gray-700 border-gray-200">
                            <td>{user.id}</td>
                            <td>{user.name}</td>
                            <td><Link href={`/admin/user/${user.id}`}>View</Link></td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
