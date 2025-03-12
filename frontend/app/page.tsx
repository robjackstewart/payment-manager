'use server';

import Link from "next/link";

export default async function Home() {
  return (
    <div className="relative overflow-x-auto">
      <h1>Payment Manager</h1>
      <div><Link href="/users">Users</Link></div>
    </div>
  );
}
