import { Suspense } from "react";
import EnrollForm from "./EnrollForm";

export default async function EnrollPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;

  return (
    <Suspense fallback={null}>
      <EnrollForm locale={locale} />
    </Suspense>
  );
}
