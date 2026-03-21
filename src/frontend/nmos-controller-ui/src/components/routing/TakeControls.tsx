interface TakeControlsProps {
  autoTake: boolean;
  hasPreview: boolean;
  isBusy: boolean;
  onToggleAutoTake: (value: boolean) => void;
  onTake: () => void;
  onClear: () => void;
  onDisconnect: () => void;
}

export function TakeControls({
  autoTake,
  hasPreview,
  isBusy,
  onToggleAutoTake,
  onTake,
  onClear,
  onDisconnect,
}: TakeControlsProps) {
  return (
    <div className="take-controls">
      <label className="take-checkbox">
        <input type="checkbox" checked={autoTake} onChange={(event) => onToggleAutoTake(event.target.checked)} />
        <span>Auto-take</span>
      </label>
      <button className="ghost-button" type="button" onClick={onClear} disabled={isBusy || !hasPreview}>
        Clear
      </button>
      <button className="ghost-button" type="button" onClick={onDisconnect} disabled={isBusy}>
        Disconnect
      </button>
      <button className="primary-button" type="button" onClick={onTake} disabled={isBusy || !hasPreview}>
        {isBusy ? "Taking..." : "TAKE"}
      </button>
    </div>
  );
}
