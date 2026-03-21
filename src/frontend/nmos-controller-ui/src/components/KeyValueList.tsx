interface KeyValueItem {
  label: string;
  value: string;
}

interface KeyValueListProps {
  items: KeyValueItem[];
}

export function KeyValueList({ items }: KeyValueListProps) {
  return (
    <dl className="kv-list">
      {items.map((item) => (
        <div key={`${item.label}-${item.value}`} className="kv-row">
          <dt>{item.label}</dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}
