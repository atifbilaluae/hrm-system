import { NextResponse, type NextRequest } from 'next/server';

const ACCESS_TOKEN_COOKIE = 'hrm_access_token';
const PUBLIC_PATHS = ['/login'];

/**
 * Edge-level route guard. It only checks that a token cookie exists so an
 * unauthenticated visitor never sees a dashboard shell. Real authorization
 * still happens in each microservice on every API call.
 */
export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const hasToken = Boolean(request.cookies.get(ACCESS_TOKEN_COOKIE)?.value);
  const isPublic = PUBLIC_PATHS.some((path) => pathname.startsWith(path));

  if (!hasToken && !isPublic) {
    const loginUrl = new URL('/login', request.url);
    loginUrl.searchParams.set('next', pathname);
    return NextResponse.redirect(loginUrl);
  }

  if (hasToken && isPublic) {
    return NextResponse.redirect(new URL('/dashboard', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico|.*\\.svg).*)'],
};
