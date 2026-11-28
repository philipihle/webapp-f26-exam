import { useState } from 'react';
import { Form, Button, Alert } from 'react-bootstrap';
import { useAuth } from './AuthContext';
import * as authService from './AuthService';

export default function Account() {
  const { user } = useAuth();
  const [email, setEmail] = useState(user?.email ?? '');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    if (password && password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setIsSubmitting(true);
    try {
      await authService.updateAccount({
        email,
        password: password || undefined,
        confirmPassword: confirmPassword || undefined,
      });
      setPassword('');
      setConfirmPassword('');
      setSuccess('Account updated successfully.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update account.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="row justify-content-center py-4">
      <div className="col-md-6 col-lg-5">
        <div className="card p-4 shadow-sm">
          <h3 className="mb-1">Account Settings</h3>
          <p className="text-muted mb-4">Update your email or change your password</p>

          {error && <Alert variant="danger">{error}</Alert>}
          {success && <Alert variant="success">{success}</Alert>}

          <Form onSubmit={handleSubmit}>
            <Form.Group className="mb-3" controlId="formAccountEmail">
              <Form.Label>Email</Form.Label>
              <Form.Control type="email" value={email} onChange={(e) => setEmail(e.target.value)} disabled={isSubmitting} required />
            </Form.Group>

            <Form.Group className="mb-3" controlId="formAccountPassword">
              <Form.Label>New Password</Form.Label><Form.Text className="text-muted small p-2">Optional*</Form.Text>
              <Form.Control type="password" placeholder="Leave blank to keep current" value={password} onChange={(e) => setPassword(e.target.value)} disabled={isSubmitting} minLength={6} />
              <Form.Text muted>At least 6 characters, one digit and one uppercase letter.</Form.Text>
            </Form.Group>

            <Form.Group className="mb-4" controlId="formAccountConfirm">
              <Form.Label>Confirm New Password</Form.Label>
              <Form.Control type="password" placeholder="Repeat the new password to confirm" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} disabled={isSubmitting} />
            </Form.Group>

            <Button variant="primary" type="submit" className="w-100" disabled={isSubmitting}>
                {isSubmitting ? 'Saving...' : 'Save Changes'}
            </Button>
          </Form>
        </div>
      </div>
    </div>
  );
}