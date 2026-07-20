import { NextRequest } from "next/server";
import { logout } from "../_lib/oauth";

export const dynamic = "force-dynamic";

export async function POST(request: NextRequest) {
  return logout(request);
}
