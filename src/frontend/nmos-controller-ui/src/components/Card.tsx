import type { ReactNode } from "react";

interface CardProps {
  title?: string;
  subtitle?: string;
  actions?: ReactNode;
  className?: string;
  children: ReactNode;
}

export function Card({ title, subtitle, actions, className, children }: CardProps) {
  const cardClassName = className ? `card ${className}` : "card";

  return (
    <section className={cardClassName}>
      {(title || actions) && (
        <header className="card-header">
          <div>
            {title ? <h3>{title}</h3> : null}
            {subtitle ? <p>{subtitle}</p> : null}
          </div>
          {actions ? <div className="card-actions">{actions}</div> : null}
        </header>
      )}
      {children}
    </section>
  );
}
