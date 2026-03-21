interface ErrorPanelProps {
  message: string;
}

export function ErrorPanel({ message }: ErrorPanelProps) {
  return (
    <div className="error-panel">
      <strong>Request failed</strong>
      <p>{message}</p>
    </div>
  );
}
