import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Form, Button, Alert, Spinner } from 'react-bootstrap';
import { useAuth } from './AuthContext';

// Login form. Layout and classes follow Views/Account/Login.cshtml in SubTask1.
export default function LoginPage() {
  const [email, setEmail] = useState<string>('');
  const [password, setPassword] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login({ email, password });
      navigate('/rooms');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Invalid email or password.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="row justify-content-center py-5">
      <div className="col-md-5 col-lg-4">
        <div className="card border-0 shadow-sm rounded-4 p-4 p-md-5 bg-white">
          <div className="text-center mb-4">
            <h3 className="fw-bold mb-1">Sign In</h3>
            <p className="text-muted small">Access your study room reservations</p>
          </div>

          {error && <Alert variant="danger" className="py-2 px-3 small border-0 rounded-3">{error}</Alert>}

          <Form onSubmit={handleSubmit}>
            <Form.Group className="mb-3" controlId="formLoginEmail">
              <Form.Label className="small fw-semibold text-muted">Email</Form.Label>
              <Form.Control
                type="email"
                placeholder="name@domain.no"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isSubmitting}
                required
                autoFocus
              />
            </Form.Group>

            <Form.Group className="mb-4" controlId="formLoginPassword">
              <Form.Label className="small fw-semibold text-muted">Password</Form.Label>
              <Form.Control
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={isSubmitting}
                required
              />
            </Form.Group>

            <Button variant="primary" type="submit" className="w-100 mb-3" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Spinner animation="border" size="sm" className="me-2" />
                  Signing in...
                </>
              ) : (
                'Sign in'
              )}
            </Button>

            <div className="text-center small text-muted">
              New here? <Link to="/register" className="text-decoration-none fw-semibold text-primary">Create an account</Link>
            </div>
          </Form>
        </div>
      </div>
    </div>
  );
}