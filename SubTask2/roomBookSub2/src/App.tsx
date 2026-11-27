import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import RoomListPage from './rooms/RoomListPage';
import RoomCreatePage from './rooms/RoomCreatePage';
import RoomUpdatePage from './rooms/RoomUpdatePage';
import NavMenu from './shared/NavMenu';
import Footer from './shared/Footer';
import { AuthProvider } from './auth/AuthContext';
import LoginPage from './auth/LoginPage';
import RegisterPage from './auth/RegisterPage';
import BookingCreatePage from './bookings/BookingCreatePage';
import MyBookingsPage from './bookings/MyBookingsPage';
import ProtectedRoute from './auth/ProtectedRoute';
import Account from './auth/Account';

const queryClient = new QueryClient(); // one shared instance for the whole app

// Page layout follows _Layout.cshtml in SubTask1: navbar, main content, footer
export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <Router>
          <div className="d-flex flex-column min-vh-100">
            <NavMenu />
            <div className="container-xl flex-grow-1 mt-4">
              <main role="main" className="pb-4">
                <Routes>
                  <Route path="/login" element={<LoginPage />} />
                  <Route path="/register" element={<RegisterPage />} />
                  {/* Only logged in users can access the paths below, hence the ProtectedRoute wrapper */}
                  <Route element={<ProtectedRoute />}>
                    <Route path="/rooms" element={<RoomListPage />} />
                    <Route path="/roomcreate" element={<RoomCreatePage />} />
                    <Route path="/roomupdate/:roomId" element={<RoomUpdatePage />} />
                    <Route path="/book/:roomId" element={<BookingCreatePage />} />
                    <Route path="/mybookings" element={<MyBookingsPage />} />
                    <Route path="/account" element={<Account />} />
                  </Route>
                  <Route path="*" element={<Navigate to="/rooms" replace />} />
                </Routes>
              </main>
            </div>
            <Footer />
          </div>
        </Router>
      </AuthProvider>
    </QueryClientProvider>
  );
}
