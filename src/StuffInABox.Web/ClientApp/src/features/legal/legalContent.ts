import type { Lang } from '../../i18n'
import type { LegalPage } from '../../store/uiStore'

/**
 * Terms of Service + Privacy Policy content, per language. This is a solid baseline
 * that reflects how the app actually handles data — but it should be reviewed by
 * legal counsel and the [placeholders] filled in before launch.
 */
export interface LegalSection {
  heading: string
  paragraphs: string[]
}
export interface LegalDoc {
  title: string
  updated: string
  intro: string
  sections: LegalSection[]
}

const LAST_UPDATED = '2026-06-22'

const COMPANY = '[Företagsnamn]'
const CONTACT = '[kontakt@dindomän.se]'

const sv: Record<LegalPage, LegalDoc> = {
  terms: {
    title: 'Användarvillkor',
    updated: LAST_UPDATED,
    intro: 'Genom att skapa ett konto och använda StuffInABox godkänner du dessa villkor.',
    sections: [
      {
        heading: 'Tjänsten',
        paragraphs: [
          'StuffInABox hjälper dig att katalogisera fysisk förvaring — utrymmen, lådor och föremål — och söka i registret. Tjänsten tillhandahålls i befintligt skick, utan garantier för oavbruten tillgänglighet eller fullständig felfrihet.',
        ],
      },
      {
        heading: 'Ditt konto',
        paragraphs: [
          'Du ansvarar för att hålla dina inloggningsuppgifter säkra och för all aktivitet som sker via ditt konto. Meddela oss omgående om du misstänker obehörig åtkomst.',
        ],
      },
      {
        heading: 'Ditt innehåll',
        paragraphs: [
          'Du behåller äganderätten till det innehåll du lägger in (namn, taggar, foton m.m.). Du ansvarar för att innehållet är lagligt och inte kränker andras rättigheter. Delar du ett utrymme ger du inbjudna användare rätt att se och redigera innehållet i det utrymmet.',
        ],
      },
      {
        heading: 'Acceptabel användning',
        paragraphs: [
          'Använd inte tjänsten för olagliga ändamål, försök inte få obehörig åtkomst till andra konton eller system, och stör inte driften.',
        ],
      },
      {
        heading: 'Uppsägning',
        paragraphs: [
          'Du kan när som helst radera ditt konto under Inställningar → Konto & data. Vi förbehåller oss rätten att stänga av konton som bryter mot dessa villkor.',
        ],
      },
      {
        heading: 'Ansvarsbegränsning',
        paragraphs: [
          'I den utsträckning lagen tillåter ansvarar vi inte för indirekta skador eller förlorad data. Exportera viktig data regelbundet (Inställningar → Konto & data).',
        ],
      },
      {
        heading: 'Ändringar och tillämplig lag',
        paragraphs: [
          'Vi kan uppdatera dessa villkor. Väsentliga ändringar meddelas i appen, och fortsatt användning innebär att du godkänner de uppdaterade villkoren. Svensk lag tillämpas.',
          `Frågor? Kontakta ${COMPANY} på ${CONTACT}.`,
        ],
      },
    ],
  },
  privacy: {
    title: 'Integritetspolicy',
    updated: LAST_UPDATED,
    intro: `Den här policyn beskriver hur StuffInABox samlar in och behandlar dina personuppgifter. Personuppgiftsansvarig är ${COMPANY} (${CONTACT}).`,
    sections: [
      {
        heading: 'Uppgifter vi behandlar',
        paragraphs: [
          'Kontouppgifter: Registrerar du dig med e-post lagrar vi din e-postadress (för inloggning och nödvändig kommunikation, t.ex. lösenordsåterställning) samt ditt lösenord i hashad form (BCrypt). Loggar du in med Google eller Apple lagrar vi endast ett anonymt id från leverantören — ingen e-post och inget namn.',
          'Innehåll du skapar: utrymmen, lådor, föremål, taggar och foton du laddar upp.',
          'Inställningar: tema, design och språk.',
          'Teknisk data: tidsbegränsade inloggnings- och sessions-tokens (lagras hashade) samt serverloggar för drift och säkerhet.',
        ],
      },
      {
        heading: 'Foton',
        paragraphs: [
          'När du laddar upp ett foto tas inbäddad metadata (t.ex. EXIF och GPS-position) bort automatiskt innan bilden sparas.',
        ],
      },
      {
        heading: 'AI-funktioner',
        paragraphs: [
          'Om automatisk bildigenkänning är aktiverad analyseras foton för att föreslå namn och taggar. I standardkonfigurationen sker detta lokalt på vår egen server. Om en extern AI-leverantör är aktiverad kan föremålsnamn eller bilder skickas dit för bearbetning.',
        ],
      },
      {
        heading: 'Delning',
        paragraphs: [
          'Om du skapar en delningslänk till ett utrymme kan användare som accepterar länken se och redigera innehållet i det utrymmet tills du återkallar länken eller tar bort dem.',
        ],
      },
      {
        heading: 'Cookies',
        paragraphs: [
          'Vi använder en enda nödvändig, säker cookie (HttpOnly) för att hålla dig inloggad. Vi använder inga cookies för spårning eller analys.',
        ],
      },
      {
        heading: 'Lagring och säkerhet',
        paragraphs: [
          'All trafik överförs krypterat (HTTPS). Lösenord och tokens lagras aldrig i klartext. Vi sparar dina uppgifter så länge du har ett konto — när du raderar kontot tas all din data bort permanent.',
        ],
      },
      {
        heading: 'Dina rättigheter (GDPR)',
        paragraphs: [
          'Du har rätt till tillgång, rättelse och radering av dina personuppgifter. Direkt i appen kan du när som helst exportera all din data eller radera ditt konto under Inställningar → Konto & data.',
          `För övriga frågor om dina uppgifter, eller för att lämna klagomål, kontakta ${CONTACT}. Du har även rätt att vända dig till Integritetsskyddsmyndigheten (IMY).`,
        ],
      },
      {
        heading: 'Ändringar',
        paragraphs: [
          'Vi kan uppdatera denna policy. Väsentliga ändringar meddelas i appen.',
        ],
      },
    ],
  },
}

const en: Record<LegalPage, LegalDoc> = {
  terms: {
    title: 'Terms of Service',
    updated: LAST_UPDATED,
    intro: 'By creating an account and using StuffInABox you agree to these terms.',
    sections: [
      {
        heading: 'The service',
        paragraphs: [
          'StuffInABox helps you catalogue physical storage — spaces, boxes and items — and search the index. The service is provided as is, without guarantees of uninterrupted availability or being entirely error-free.',
        ],
      },
      {
        heading: 'Your account',
        paragraphs: [
          'You are responsible for keeping your credentials secure and for all activity under your account. Tell us promptly if you suspect unauthorised access.',
        ],
      },
      {
        heading: 'Your content',
        paragraphs: [
          'You retain ownership of the content you add (names, tags, photos, etc.). You are responsible for ensuring it is lawful and does not infringe others’ rights. Sharing a space grants invited users the right to view and edit the content in that space.',
        ],
      },
      {
        heading: 'Acceptable use',
        paragraphs: [
          'Do not use the service for unlawful purposes, attempt to gain unauthorised access to other accounts or systems, or disrupt operation.',
        ],
      },
      {
        heading: 'Termination',
        paragraphs: [
          'You can delete your account at any time under Settings → Account & data. We reserve the right to suspend accounts that violate these terms.',
        ],
      },
      {
        heading: 'Limitation of liability',
        paragraphs: [
          'To the extent permitted by law, we are not liable for indirect damages or lost data. Export important data regularly (Settings → Account & data).',
        ],
      },
      {
        heading: 'Changes and governing law',
        paragraphs: [
          'We may update these terms. Material changes are announced in the app, and continued use means you accept the updated terms. Swedish law applies.',
          `Questions? Contact ${COMPANY} at ${CONTACT}.`,
        ],
      },
    ],
  },
  privacy: {
    title: 'Privacy Policy',
    updated: LAST_UPDATED,
    intro: `This policy describes how StuffInABox collects and processes your personal data. The data controller is ${COMPANY} (${CONTACT}).`,
    sections: [
      {
        heading: 'Data we process',
        paragraphs: [
          'Account data: If you register with email we store your email address (for sign-in and essential communication such as password resets) and your password in hashed form (BCrypt). If you sign in with Google or Apple we store only an anonymous id from the provider — no email or name.',
          'Content you create: spaces, boxes, items, tags and photos you upload.',
          'Settings: theme, design and language.',
          'Technical data: time-limited sign-in and session tokens (stored hashed) and server logs for operations and security.',
        ],
      },
      {
        heading: 'Photos',
        paragraphs: [
          'When you upload a photo, embedded metadata (such as EXIF and GPS location) is automatically removed before the image is stored.',
        ],
      },
      {
        heading: 'AI features',
        paragraphs: [
          'If automatic image recognition is enabled, photos are analysed to suggest names and tags. In the default configuration this happens locally on our own server. If an external AI provider is enabled, item names or images may be sent there for processing.',
        ],
      },
      {
        heading: 'Sharing',
        paragraphs: [
          'If you create a share link to a space, users who accept it can view and edit the content in that space until you revoke the link or remove them.',
        ],
      },
      {
        heading: 'Cookies',
        paragraphs: [
          'We use a single necessary, secure cookie (HttpOnly) to keep you signed in. We do not use any tracking or analytics cookies.',
        ],
      },
      {
        heading: 'Storage and security',
        paragraphs: [
          'All traffic is transmitted encrypted (HTTPS). Passwords and tokens are never stored in plaintext. We keep your data for as long as you have an account — when you delete your account, all your data is permanently removed.',
        ],
      },
      {
        heading: 'Your rights (GDPR)',
        paragraphs: [
          'You have the right to access, rectify and erase your personal data. Right in the app you can export all your data or delete your account at any time under Settings → Account & data.',
          `For other questions about your data, or to file a complaint, contact ${CONTACT}. You also have the right to lodge a complaint with the Swedish Authority for Privacy Protection (IMY).`,
        ],
      },
      {
        heading: 'Changes',
        paragraphs: [
          'We may update this policy. Material changes are announced in the app.',
        ],
      },
    ],
  },
}

const LEGAL: Record<Lang, Record<LegalPage, LegalDoc>> = { sv, en }

export function getLegalDoc(lang: Lang, page: LegalPage): LegalDoc {
  return LEGAL[lang][page]
}
