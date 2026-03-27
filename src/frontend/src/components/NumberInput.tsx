import { useEffect, useState } from "react";

type Props = {
  value: number;
  onChange: (val: number) => void;
  className?: string;
  placeholder?: string;
  min?: number;
  max?: number;
  step?: number;
};

export function NumberInput({ value, onChange, className, placeholder, min, max, step }: Props) {
  const [text, setText] = useState(String(value));

  useEffect(() => {
    setText(String(value));
  }, [value]);

  return (
    <input
      type="number"
      className={className}
      placeholder={placeholder}
      min={min}
      max={max}
      step={step}
      value={text}
      onChange={(e) => {
        setText(e.target.value);
        const num = Number(e.target.value);
        if (e.target.value !== "" && !isNaN(num)) {
          onChange(num);
        }
      }}
      onBlur={() => {
        if (text === "" || isNaN(Number(text))) {
          setText(String(value));
        }
      }}
    />
  );
}
