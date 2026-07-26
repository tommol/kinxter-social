import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Kinxter — społeczność bez oceniania",
  description:
    "Prywatna platforma społecznościowa dla pełnoletnich osób ze społeczności kink i fetysz.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="pl">
      <body>{children}</body>
    </html>
  );
}
