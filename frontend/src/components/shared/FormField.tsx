import { InputHTMLAttributes } from "react";

interface FormFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
}

export default function FormField({
  label,
  error,
  ...props
}: FormFieldProps) {
  return (
    <div className="space-y-1">
      <label className="text-sm font-medium text-slate-700">{label}</label>
      <input
        {...props}
        className={`w-full rounded-lg border px-3 py-2 text-sm outline-none transition-colors ${
          error
            ? "border-rose-300 bg-rose-50 focus:border-rose-500 focus:ring-2 focus:ring-rose-100"
            : "border-slate-200 bg-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
        }`}
      />
      {error && <p className="text-xs text-rose-600">{error}</p>}
    </div>
  );
}
