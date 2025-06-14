import type { ReactNode } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './card';

interface EmptyStateProps {
  icon?: ReactNode;
  title: string;
  description: string;
  action?: ReactNode;
  className?: string;
}

export function EmptyState({ 
  icon, 
  title, 
  description, 
  action, 
  className = "" 
}: EmptyStateProps) {
  return (
    <Card className={`border-dashed ${className}`}>
      <CardHeader className="text-center pb-2">
        {icon && (
          <div className="mx-auto mb-4 p-3 bg-muted rounded-full w-fit">
            {icon}
          </div>
        )}
        <CardTitle className="text-xl">{title}</CardTitle>
        <CardDescription className="text-base">
          {description}
        </CardDescription>
      </CardHeader>
      {action && (
        <CardContent className="text-center pt-0">
          {action}
        </CardContent>
      )}
    </Card>
  );
}