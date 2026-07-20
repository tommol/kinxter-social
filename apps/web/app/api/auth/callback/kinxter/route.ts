import { NextRequest } from "next/server";
import { completeLogin } from "../../_lib/oauth";

export const dynamic = "force-dynamic";

export async function GET(request: NextRequest) {
  return completeLogin(request);
}
