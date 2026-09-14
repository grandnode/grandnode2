//All configuration comes from environment variables so no URL, account or code of a
//real installation ends up in the repository.
const read = name => process.env[name]?.trim() || undefined

export const env = {
    baseUrl: read('GRAND_ADMIN_URL') ?? 'http://localhost:5000',
    credentials: {
        admin: { email: read('GRAND_ADMIN_EMAIL'), password: read('GRAND_ADMIN_PASSWORD') },
        store: { email: read('GRAND_STORE_EMAIL'), password: read('GRAND_STORE_PASSWORD') },
        vendor: { email: read('GRAND_VENDOR_EMAIL'), password: read('GRAND_VENDOR_PASSWORD') }
    },
    //SEO codes (Language.UniqueSeoCode, e.g. en, pl, ar) of the languages to switch to.
    //Leave GRAND_LANGUAGE_CODE_EN empty to run en-US in whatever language the account
    //already uses; the pl-PL and RTL projects are skipped until their code is set.
    languageCodes: {
        en: read('GRAND_LANGUAGE_CODE_EN'),
        pl: read('GRAND_LANGUAGE_CODE_PL'),
        rtl: read('GRAND_LANGUAGE_CODE_RTL')
    }
}

export const LOCALES = [
    { name: 'en-US', browserLocale: 'en-US', languageCode: env.languageCodes.en, rtl: false, optional: false },
    { name: 'pl-PL', browserLocale: 'pl-PL', languageCode: env.languageCodes.pl, rtl: false, optional: true },
    { name: 'rtl', browserLocale: 'ar-SA', languageCode: env.languageCodes.rtl, rtl: true, optional: true }
]

export const PANELS = {
    admin: { area: 'Admin', loginPath: '/admin/login/' },
    store: { area: 'Store', loginPath: '/store/login/' },
    vendor: { area: 'Vendor', loginPath: '/vendor/login/' }
}

export const hasCredentials = panel => !!(env.credentials[panel].email && env.credentials[panel].password)
