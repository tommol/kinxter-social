import { NextRequest } from "next/server";
import { startLogin } from "../_lib/oauth";

export const dynamic = "force-dynamic";

export async function GET(request: NextRequest) {
  return startLogin(request, ["openid", "profile", "email", "offline_access", "kinxter.api"]);
}
