// The logged-in user, read from the JWT with jwt-decode (claims set in AuthController.GenerateJwtToken)
export interface User {
  sub: string; // username
  email: string;
  nameid: string; // user id
  jti: string; // unique token id
  iat: number; // issued at (unix seconds)
  exp: number; // expires at (unix seconds)
  iss: string; // issuer
  aud: string; // audience
  role?: string; // "Admin" or "Student"
}
