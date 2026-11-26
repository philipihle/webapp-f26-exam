import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import RoomListPage from './rooms/RoomListPage';
import RoomCreatePage from './rooms/RoomCreatePage';
import RoomUpdatePage from './rooms/RoomUpdatePage';
import NavMenu from './shared/NavMenu';
import Footer from './shared/Footer';
import { AuthProvider } from './auth/AuthContext';

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
                  <Route path="/rooms" element={<RoomListPage />} />
                  <Route path="/roomcreate" element={<RoomCreatePage />} />
                  <Route path="/roomupdate/:roomId" element={<RoomUpdatePage />} />
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
