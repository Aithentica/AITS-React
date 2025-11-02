import { render, screen } from '@testing-library/react'
import App from './App'

vi.mock('./i18n', () => ({ loadTranslations: async () => ({ 'login.title': 'Logowanie', 'login.email': 'E-mail', 'login.password': 'Hasło', 'login.submit': 'Zaloguj' }) }))

it('renderuje formularz logowania', async () => {
  render(<App />)
  expect(await screen.findByText(/Logowanie|Sign in/i)).toBeInTheDocument()
  expect(screen.getByText(/E-mail|Email/i)).toBeInTheDocument()
})




