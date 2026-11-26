import { Link } from 'react-router-dom';
import { Button } from 'react-bootstrap';
import { useAuth } from './AuthContext';

// Right side of the navbar: username + Logout when logged in, otherwise Login/Register
export default function AuthSection() {
  const { user, logout } = useAuth();

  return (
    <div className="d-flex flex-wrap align-items-center gap-2 ms-auto">
      {user ? (
        <>
          <div className="d-none d-sm-flex flex-column text-end px-2">
            <span className="fw-semibold small text-dark">{user.sub}</span>
            <span className="text-muted small">Student</span>
          </div>
          <Button variant="outline-secondary" size="sm" onClick={logout}>
            Logout
          </Button>
        </>
      ) : (
        <>
          <Link to="/login" className="btn btn-outline-secondary btn-sm">Login</Link>
          <Link to="/register" className="btn btn-primary btn-sm">Register</Link>
        </>
      )}
    </div>
  );
}