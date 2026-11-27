import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Form, Button, Alert, Spinner } from 'react-bootstrap';
import * as authService from './AuthService';

// Registration form. Layout and classes follow Views/Account/Register.cshtml in SubTask1.
export default function RegisterPage() {
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await authService.register({ username, email, password });
      navigate('/login'); // the user logs in after registering
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="row justify-content-center py-5">
      <div className="col-md-6 col-lg-5">
        <div className="card border-0 shadow-sm rounded-4 p-4 p-md-5">
          <div className="text-center mb-4">
            <div className="p-2 rounded-3 bg-dark text-white d-inline-flex align-items-center justify-content-center mb-2" style={{ width: '42px', height: '42px' }}>
              <i className="bi bi-person-plus fs-5" />
            </div>
            <h3 className="fw-bold mb-1">Create Account</h3>
            <p className="text-muted small">Register as a student to book campus study rooms</p>
          </div>

          {error && <Alert variant="danger" className="py-2 small">{error}</Alert>}

          <Form onSubmit={handleSubmit}>
            <Form.Group className="mb-3" controlId="formRegisterUsername">
              <Form.Label className="small fw-semibold text-muted">Username</Form.Label>
              <Form.Control type="text" placeholder="E.g Erling Haaland" value={username} onChange={(e) => setUsername(e.target.value)} disabled={isSubmitting} required autoFocus />
            </Form.Group>

            <Form.Group className="mb-3" controlId="formRegisterEmail">
              <Form.Label className="small fw-semibold text-muted">Email</Form.Label>
              <Form.Control type="email" placeholder="youremail@student.no" value={email} onChange={(e) => setEmail(e.target.value)} disabled={isSubmitting} required />
            </Form.Group>

            <Form.Group className="mb-4" controlId="formRegisterPassword">
              <Form.Label className="small fw-semibold text-muted">Password</Form.Label>
              <Form.Control type="password" placeholder="••••••••" value={password} onChange={(e) => setPassword(e.target.value)} disabled={isSubmitting} required minLength={6} />
              <Form.Text className="text-muted small">At least 6 characters, one digit and one uppercase letter.</Form.Text>
            </Form.Group>

            <Button variant="primary" type="submit" className="w-100 mb-3" disabled={isSubmitting}>
              {isSubmitting ? <><Spinner animation="border" size="sm" className="me-2" />Creating account...</> : 'Create account'}
            </Button>

            <div className="text-center small text-muted">
              Already have an account? <Link to="/login" className="text-decoration-none fw-semibold">Sign in</Link>
            </div>
          </Form>
        </div>
      </div>
    </div>
  );
}