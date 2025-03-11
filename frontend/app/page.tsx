import { createUser } from "@/app/actions";

async function createUserOnClick() {
  await createUser({ name: "Test user" });
}

export default function Home() {
  return (
    <button onClick={createUserOnClick}>Create</button>
  );
}
