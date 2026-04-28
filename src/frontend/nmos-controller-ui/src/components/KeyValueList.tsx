interface KeyValueItem {
  label: string;
  value: string;
}

interface KeyValueListProps {
  items: KeyValueItem[];
  className?: string;
}

export function KeyValueList({ items, className }: KeyValueListProps) {
  const classes = className ? `kv-list ${className}` : "kv-list";
  return (
    <dl className={classes}>
      {items.map((item) => (
        <div key={`${item.label}-${item.value}`} className="kv-row">
          <dt>{item.label}</dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}
