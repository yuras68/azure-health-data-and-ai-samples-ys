import React, { useEffect, useState, FC, ReactElement } from 'react';
import { Stack, Text, List, IStackStyles, PrimaryButton, DefaultButton, Checkbox, Spinner, SpinnerSize } from '@fluentui/react';

import { AppConsentInfo, AppConsentScope } from '../AppContext';

const moduleStyle: IStackStyles = {
    root: {
        paddingBottom: 20,
    }
}

/**UI improvements */
const ysStackTokens = { childrenGap: 30 };
const ysTableStackTokens = { childrenGap: 20 };

interface ScopeSelectorProps {
    consentInfo?: AppConsentInfo
    requestedScopes: string[] | undefined
    updateUserApprovedScopes?: (scopes: AppConsentInfo) => Promise<void>
}

export const ScopeSelector: FC<ScopeSelectorProps> = (props: ScopeSelectorProps): ReactElement => {
    const [consentInfo, setConsentInfo] = useState(props.consentInfo);
    const [requestedScopes, setRequestedScopes] = useState(props.requestedScopes);
    const [mode, setMode] = useState("loading");

    useEffect(() => {
        setConsentInfo(props.consentInfo);

        // #TODO - check other state elements (like update function) before changing state from loading

        // Set the initial state value
        if (props.consentInfo && mode === "loading") {
            if (props.consentInfo.scopes.filter(x => x.consented).length > 0) {
                setMode('existing review');
            }
            else {
                setMode('new edit');
            }
        }
    }, [props]);

    const changeEditMode = () => {
        setMode('existing edit');
    };

    const handleScopeChecked = (scope: AppConsentScope) => {
        return (ev?: React.FormEvent<HTMLElement | HTMLInputElement>, isChecked?: boolean) => {

            if (consentInfo !== undefined) {
                scope.enabled = isChecked!;
                const updateConsentInfo = {
                    ...consentInfo,
                    // only update the scope that was changed
                    scopes: consentInfo!.scopes.map(x => x.name === scope.name && x.resourceId === scope.resourceId ? scope : x),
                };
                setConsentInfo(updateConsentInfo);
            }
        }
    }

    const updateScopes = () => {
        setMode('redirecting');
        props.updateUserApprovedScopes!(consentInfo!);
    };

    /** UI improvements */
    const checkAll = (): void => {
        setAllConsented(true);
    }
    const uncheckAll = (): void => {
        setAllConsented(false);
    }

    const setAllConsented = (newValue: boolean) => {
        if (consentInfo !== undefined) {
            const updateConsentInfo = {
                ...consentInfo,
                // yshalak - some scopes should not be "unconsented"
                //scopes: consentInfo!.scopes.map(x => { x.enabled = newValue; return x; }),
                scopes: consentInfo!.scopes.map(x => { x.enabled = newValue || x.name.includes('fhirUser') || x.name.includes('launch') || x.name.includes('openid'); return x; }),
            };
            setConsentInfo(updateConsentInfo);
        }
    }

    const setLimited = () => {
        if (consentInfo !== undefined) {
            const updateConsentInfo = {
                ...consentInfo,
                // Patient, Condition, Observation
                scopes: consentInfo!.scopes.map(x => {
                    x.enabled = x.name.includes('Patient.read') || x.name.includes('Condition.read') || x.name.includes('Observation.read')
                        || x.name.includes('fhirUser') || x.name.includes('launch') || x.name.includes('openid');
                    return x;
                }),
            };
            setConsentInfo(updateConsentInfo);
        }
    }

    /** UI improvements */
    return (
        <Stack>
            <Stack.Item align='start'>
                {(mode === 'loading') && <Spinner size={SpinnerSize.large} label="Loading..." ariaLive="assertive" />}
                {(mode === 'redirecting') && <Spinner size={SpinnerSize.large} label="Saving your preferences...this may take a bit..." ariaLive="assertive" />}
            </Stack.Item>
            <Stack.Item>
                <Stack horizontal tokens={ysTableStackTokens}>
                    {(mode.includes('existing') || mode.includes('new')) &&
                        <Stack.Item styles={moduleStyle}>
                            <Text block variant="xLarge">Requested Access:</Text>
                            <List items={requestedScopes?.map(x => ({ name: x }))} />
                        </Stack.Item>
                    }

                    {mode.includes('existing') &&
                        <Stack.Item styles={moduleStyle}>
                            <Text block variant="xLarge">Approved Access:</Text>
                            <List items={consentInfo?.scopes.filter(x => x.consented).filter(x => !x.hidden)} />
                        </Stack.Item>
                    }

                    {mode.includes('edit') &&
                        <Stack.Item styles={moduleStyle}>
                            <Stack horizontal tokens={ysStackTokens}>
                                <Text block variant="xLarge">Select Access:</Text>
                                <PrimaryButton onClick={checkAll} text="Set all" ></PrimaryButton>
                                <DefaultButton onClick={uncheckAll} text="Clear all" ></DefaultButton>
                                {/*<DefaultButton onClick={setLimited} text="Set Limited" ></DefaultButton>*/}
                            </Stack>
                            {consentInfo?.scopes.map((scope) => (
                                //scope.hidden ? null : <Checkbox key={scope.id} label={scope.name} checked={scope.enabled} onChange={handleScopeChecked(scope)} />
                                scope.hidden ? null : <Checkbox key={scope.id} label={scope.name.replace('patient.', 'patient/')} checked={scope.enabled} onChange={handleScopeChecked(scope)} />
                            ))}
                        </Stack.Item>
                    }

                    {mode !== 'loading' && mode !== 'redirecting' &&
                        <Stack.Item styles={moduleStyle}>
                            <Stack horizontal>
                                <PrimaryButton text="Continue" onClick={updateScopes} />

                                {mode === 'existing review' && <DefaultButton text="Change Access" onClick={changeEditMode} />}
                            </Stack>
                        </Stack.Item>
                    }
                </Stack>
            </Stack.Item>
        </Stack>
    )
}

export default ScopeSelector;