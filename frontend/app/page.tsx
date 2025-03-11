'use client';
import { createUser } from "@/app/actions";

async function createUserOnClick() {
  await createUser({ name: "Test user" }).catch((error) => {
    console.error("Failed to create user", error);
  });
}

export default function Home() {
  return (
    <button onClick={createUserOnClick}>Create User</button>
  );
}
